#include "d3d12_render_target.h"
#include "d3d12_resources.h"
#include "d3dx12.h"
#include <cstring>
#include <cstdio>
#include <algorithm>

// Define INITGUID before including d2d1effects.h to get CLSID definitions
#include <initguid.h>
#include <d2d1effects_2.h>

using std::min;
using std::max;

namespace jalium {

D3D12RenderTarget::D3D12RenderTarget(D3D12Backend* backend, void* hwnd, int32_t width, int32_t height)
    : backend_(backend)
    , hwnd_(static_cast<HWND>(hwnd))
{
    width_ = width;
    height_ = height;
}

D3D12RenderTarget::~D3D12RenderTarget() {
    // End any ongoing drawing
    if (isDrawing_) {
        try {
            EndDraw();
        } catch (...) {}
        isDrawing_ = false;
    }

    // Flush D3D11 context
    auto d3d11Context = backend_->GetD3D11Context();
    if (d3d11Context) {
        d3d11Context->Flush();
    }

    // Wait for all GPU work to complete
    WaitForAllFrames();

    // Clear D2D target
    if (d2dContext_) {
        d2dContext_->SetTarget(nullptr);
    }

    // Release D2D resources before D3D11 wrapped resources
    for (uint32_t i = 0; i < FrameCount; ++i) {
        d2dRenderTargets_[i].Reset();
        wrappedBackBuffers_[i].Reset();
    }

    if (fenceEvent_) {
        CloseHandle(fenceEvent_);
        fenceEvent_ = nullptr;
    }
}

bool D3D12RenderTarget::Initialize() {
    if (!CreateSwapChain()) return false;
    if (!CreateRenderTargetViews()) return false;
    if (!CreateD2DRenderTarget()) return false;
    if (!CreateSnapshotResources()) return false;

    // Initialize transform stack with identity
    transformStack_.push(D2D1::Matrix3x2F::Identity());

    return true;
}

bool D3D12RenderTarget::CreateSwapChain() {
    auto device = backend_->GetDevice();
    auto commandQueue = backend_->GetCommandQueue();

    // Check for tearing support
    ComPtr<IDXGIFactory6> factory;
    HRESULT hr = CreateDXGIFactory1(IID_PPV_ARGS(&factory));
    if (FAILED(hr)) return false;

    BOOL allowTearing = FALSE;
    ComPtr<IDXGIFactory5> factory5;
    if (SUCCEEDED(factory.As(&factory5))) {
        factory5->CheckFeatureSupport(DXGI_FEATURE_PRESENT_ALLOW_TEARING, &allowTearing, sizeof(allowTearing));
    }
    tearingSupported_ = (allowTearing == TRUE);

    // Create swap chain
    DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
    swapChainDesc.Width = width_;
    swapChainDesc.Height = height_;
    swapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    swapChainDesc.SampleDesc.Count = 1;
    swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    swapChainDesc.BufferCount = FrameCount;
    swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
    swapChainDesc.Scaling = DXGI_SCALING_NONE;
    // Note: DXGI_ALPHA_MODE_PREMULTIPLIED is not supported for CreateSwapChainForHwnd
    // DWM backdrop effects work by treating black (0,0,0) as transparent in the extended frame area
    swapChainDesc.Flags = tearingSupported_ ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0;

    ComPtr<IDXGISwapChain1> swapChain1;
    hr = factory->CreateSwapChainForHwnd(
        commandQueue,
        hwnd_,
        &swapChainDesc,
        nullptr,
        nullptr,
        &swapChain1);

    if (FAILED(hr)) return false;

    // Disable Alt+Enter fullscreen
    factory->MakeWindowAssociation(hwnd_, DXGI_MWA_NO_ALT_ENTER);

    hr = swapChain1.As(&swapChain_);
    if (FAILED(hr)) return false;

    frameIndex_ = swapChain_->GetCurrentBackBufferIndex();

    // Create RTV descriptor heap
    D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
    rtvHeapDesc.NumDescriptors = FrameCount;
    rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
    rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

    hr = device->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS(&rtvHeap_));
    if (FAILED(hr)) return false;

    rtvDescriptorSize_ = device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

    // Create fence
    hr = device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&fence_));
    if (FAILED(hr)) return false;

    fenceEvent_ = CreateEventW(nullptr, FALSE, FALSE, nullptr);
    if (!fenceEvent_) return false;

    // Create command allocators
    for (uint32_t i = 0; i < FrameCount; ++i) {
        hr = device->CreateCommandAllocator(
            D3D12_COMMAND_LIST_TYPE_DIRECT,
            IID_PPV_ARGS(&commandAllocators_[i]));
        if (FAILED(hr)) return false;
    }

    // Create command list
    hr = device->CreateCommandList(
        0,
        D3D12_COMMAND_LIST_TYPE_DIRECT,
        commandAllocators_[frameIndex_].Get(),
        nullptr,
        IID_PPV_ARGS(&commandList_));
    if (FAILED(hr)) return false;

    hr = commandList_->Close();
    if (FAILED(hr)) return false;

    return true;
}

bool D3D12RenderTarget::CreateRenderTargetViews() {
    auto device = backend_->GetDevice();
    CD3DX12_CPU_DESCRIPTOR_HANDLE rtvHandle(rtvHeap_->GetCPUDescriptorHandleForHeapStart());

    for (uint32_t i = 0; i < FrameCount; ++i) {
        HRESULT hr = swapChain_->GetBuffer(i, IID_PPV_ARGS(&renderTargets_[i]));
        if (FAILED(hr)) return false;

        device->CreateRenderTargetView(renderTargets_[i].Get(), nullptr, rtvHandle);
        rtvHandle.Offset(1, rtvDescriptorSize_);
    }

    return true;
}

bool D3D12RenderTarget::CreateD2DRenderTarget() {
    auto d3d11On12 = backend_->GetD3D11On12Device();
    auto d2dDevice = backend_->GetD2DDevice();

    if (!d3d11On12 || !d2dDevice) return false;

    // Create D2D device context
    HRESULT hr = d2dDevice->CreateDeviceContext(
        D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
        &d2dContext_);

    if (FAILED(hr)) return false;

    // Set DPI
    d2dContext_->SetDpi(96.0f, 96.0f);

    // Wrap the D3D12 back buffers with D3D11 textures for D2D rendering
    D3D11_RESOURCE_FLAGS d3d11Flags = { D3D11_BIND_RENDER_TARGET };

    for (uint32_t i = 0; i < FrameCount; ++i) {
        // Wrap the D3D12 resource with a D3D11 resource
        hr = d3d11On12->CreateWrappedResource(
            renderTargets_[i].Get(),
            &d3d11Flags,
            D3D12_RESOURCE_STATE_RENDER_TARGET,
            D3D12_RESOURCE_STATE_PRESENT,
            IID_PPV_ARGS(&wrappedBackBuffers_[i]));

        if (FAILED(hr)) return false;

        // Get the DXGI surface from the wrapped resource
        ComPtr<IDXGISurface> surface;
        hr = wrappedBackBuffers_[i].As(&surface);
        if (FAILED(hr)) return false;

        // Create a D2D bitmap from the surface
        D2D1_BITMAP_PROPERTIES1 bitmapProperties = D2D1::BitmapProperties1(
            D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
            D2D1::PixelFormat(DXGI_FORMAT_R8G8B8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED),
            96.0f, 96.0f);

        hr = d2dContext_->CreateBitmapFromDxgiSurface(
            surface.Get(),
            &bitmapProperties,
            &d2dRenderTargets_[i]);

        if (FAILED(hr)) return false;
    }

    return true;
}

void D3D12RenderTarget::WaitForGpu() {
    if (!fence_ || !fenceEvent_) return;

    auto commandQueue = backend_->GetCommandQueue();
    if (!commandQueue) return;

    // Increment fence value and signal
    const uint64_t fenceValue = ++fenceValues_[frameIndex_];
    HRESULT hr = commandQueue->Signal(fence_.Get(), fenceValue);
    if (FAILED(hr)) return;

    // Wait for fence to complete
    if (fence_->GetCompletedValue() < fenceValue) {
        hr = fence_->SetEventOnCompletion(fenceValue, fenceEvent_);
        if (SUCCEEDED(hr)) {
            WaitForSingleObject(fenceEvent_, INFINITE);
        }
    }
}

void D3D12RenderTarget::WaitForAllFrames() {
    if (!fence_ || !fenceEvent_) return;

    auto commandQueue = backend_->GetCommandQueue();
    if (!commandQueue) return;

    // Find max fence value and use it
    uint64_t maxFenceValue = 0;
    for (uint32_t i = 0; i < FrameCount; ++i) {
        if (fenceValues_[i] > maxFenceValue) {
            maxFenceValue = fenceValues_[i];
        }
    }

    // Increment and signal with a new value higher than all existing
    const uint64_t fenceValue = maxFenceValue + 1;
    HRESULT hr = commandQueue->Signal(fence_.Get(), fenceValue);
    if (FAILED(hr)) return;

    // Wait for GPU to complete all work
    if (fence_->GetCompletedValue() < fenceValue) {
        hr = fence_->SetEventOnCompletion(fenceValue, fenceEvent_);
        if (SUCCEEDED(hr)) {
            WaitForSingleObject(fenceEvent_, INFINITE);
        }
    }

    // Update all fence values
    for (uint32_t i = 0; i < FrameCount; ++i) {
        fenceValues_[i] = fenceValue;
    }
}

void D3D12RenderTarget::MoveToNextFrame() {
    auto commandQueue = backend_->GetCommandQueue();
    const uint64_t currentFenceValue = fenceValues_[frameIndex_];

    commandQueue->Signal(fence_.Get(), currentFenceValue);

    frameIndex_ = swapChain_->GetCurrentBackBufferIndex();

    if (fence_->GetCompletedValue() < fenceValues_[frameIndex_]) {
        fence_->SetEventOnCompletion(fenceValues_[frameIndex_], fenceEvent_);
        // Must wait for GPU to finish with this frame's resources
        WaitForSingleObject(fenceEvent_, INFINITE);
    }

    fenceValues_[frameIndex_] = currentFenceValue + 1;
}

JaliumResult D3D12RenderTarget::Resize(int32_t width, int32_t height) {
    if (width <= 0 || height <= 0) {
        return JALIUM_ERROR_INVALID_ARGUMENT;
    }
    if (width == width_ && height == height_) {
        return JALIUM_OK;
    }

    auto d3d11On12 = backend_->GetD3D11On12Device();
    auto d3d11Context = backend_->GetD3D11Context();

    // If we're in the middle of drawing, abort it
    if (isDrawing_) {
        if (d2dContext_) {
            d2dContext_->EndDraw();
        }
        isDrawing_ = false;
    }

    // Clear D2D target before releasing resources
    if (d2dContext_) {
        d2dContext_->SetTarget(nullptr);
    }

    // Flush D3D11 context first
    if (d3d11Context) {
        d3d11Context->Flush();
    }

    // Make sure all GPU work is complete
    WaitForAllFrames();

    // Release snapshot resources
    for (uint32_t i = 0; i < FrameCount; ++i) {
        snapshotBitmaps_[i].Reset();
        snapshotTextures_[i].Reset();
        snapshotValid_[i] = false;
    }

    // Release D2D render targets (these hold references to wrapped buffers)
    for (uint32_t i = 0; i < FrameCount; ++i) {
        d2dRenderTargets_[i].Reset();
    }

    // Release D2D context
    d2dContext_.Reset();

    // Now release wrapped buffers (D3D11 resources wrapping D3D12 buffers)
    for (uint32_t i = 0; i < FrameCount; ++i) {
        wrappedBackBuffers_[i].Reset();
    }

    // Flush D3D11 again to ensure wrapped resource release is complete
    if (d3d11Context) {
        d3d11Context->Flush();
    }

    // Finally release D3D12 render targets
    for (uint32_t i = 0; i < FrameCount; ++i) {
        renderTargets_[i].Reset();
    }

    // Clear transform stack
    while (!transformStack_.empty()) {
        transformStack_.pop();
    }

    // Clear clip count
    clipCount_ = 0;

    // Resize swap chain (preserve tearing flag)
    HRESULT hr = swapChain_->ResizeBuffers(
        FrameCount,
        width, height,
        DXGI_FORMAT_R8G8B8A8_UNORM,
        tearingSupported_ ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0);

    if (FAILED(hr)) {
        return JALIUM_ERROR_RESOURCE_CREATION_FAILED;
    }

    width_ = width;
    height_ = height;
    frameIndex_ = swapChain_->GetCurrentBackBufferIndex();

    // Recreate render target views
    if (!CreateRenderTargetViews()) {
        return JALIUM_ERROR_RESOURCE_CREATION_FAILED;
    }

    // Recreate D2D context and render targets
    if (!CreateD2DRenderTarget()) {
        return JALIUM_ERROR_RESOURCE_CREATION_FAILED;
    }

    // Recreate snapshot resources
    if (!CreateSnapshotResources()) {
        return JALIUM_ERROR_RESOURCE_CREATION_FAILED;
    }

    // Reinitialize transform stack with identity (same as Initialize)
    transformStack_.push(D2D1::Matrix3x2F::Identity());

    // Flush D3D11 context to ensure wrapped resources are ready
    if (d3d11Context) {
        d3d11Context->Flush();
    }

    // Reset fence values for the new buffers
    for (uint32_t i = 0; i < FrameCount; ++i) {
        fenceValues_[i] = fenceValues_[frameIndex_];
    }

    return JALIUM_OK;
}

JaliumResult D3D12RenderTarget::BeginDraw() {
    if (isDrawing_) return JALIUM_ERROR_INVALID_STATE;

    auto d3d11On12 = backend_->GetD3D11On12Device();
    auto d3d11Context = backend_->GetD3D11Context();

    if (!d3d11On12 || !d3d11Context) return JALIUM_ERROR_INVALID_STATE;

    // Check if resources are valid
    if (!wrappedBackBuffers_[frameIndex_] || !d2dRenderTargets_[frameIndex_] || !d2dContext_) {
        return JALIUM_ERROR_INVALID_STATE;
    }

    // Acquire the wrapped back buffer for D2D rendering
    ID3D11Resource* resources[] = { wrappedBackBuffers_[frameIndex_].Get() };
    d3d11On12->AcquireWrappedResources(resources, 1);

    // Set the D2D render target
    d2dContext_->SetTarget(d2dRenderTargets_[frameIndex_].Get());

    // Begin D2D drawing
    d2dContext_->BeginDraw();

    // Set identity transform at the start of each frame
    d2dContext_->SetTransform(D2D1::Matrix3x2F::Identity());

    isDrawing_ = true;
    return JALIUM_OK;
}

JaliumResult D3D12RenderTarget::EndDraw() {
    if (!isDrawing_) return JALIUM_ERROR_INVALID_STATE;

    auto d3d11On12 = backend_->GetD3D11On12Device();
    auto d3d11Context = backend_->GetD3D11Context();

    if (!d3d11On12 || !d3d11Context) return JALIUM_ERROR_INVALID_STATE;

    // End D2D drawing
    HRESULT hr = d2dContext_->EndDraw();
    if (FAILED(hr)) return JALIUM_ERROR_DEVICE_LOST;

    // Release the D2D render target
    d2dContext_->SetTarget(nullptr);

    // Release the wrapped back buffer
    ID3D11Resource* resources[] = { wrappedBackBuffers_[frameIndex_].Get() };
    d3d11On12->ReleaseWrappedResources(resources, 1);

    // Flush the D3D11 context to submit the D2D commands
    d3d11Context->Flush();

    // Present with or without VSync
    // Use DXGI_PRESENT_ALLOW_TEARING for immediate present when VSync is off and tearing is supported
    UINT presentFlags = (!vsyncEnabled_ && tearingSupported_) ? DXGI_PRESENT_ALLOW_TEARING : 0;
    hr = swapChain_->Present(vsyncEnabled_ ? 1 : 0, presentFlags);
    if (FAILED(hr)) return JALIUM_ERROR_DEVICE_LOST;

    MoveToNextFrame();

    isDrawing_ = false;
    return JALIUM_OK;
}

void D3D12RenderTarget::Clear(float r, float g, float b, float a) {
    if (!isDrawing_) return;
    d2dContext_->Clear(D2D1::ColorF(r, g, b, a));
}

// Helper to get D2D brush without using dynamic_cast (avoids cross-DLL RTTI issues in Release mode with LTCG)
ID2D1Brush* D3D12RenderTarget::GetD2DBrush(Brush* brush) {
    if (!brush) return nullptr;

    // Use GetType() instead of dynamic_cast for cross-DLL compatibility
    switch (brush->GetType()) {
        case JALIUM_BRUSH_SOLID: {
            auto solidBrush = static_cast<D3D12SolidBrush*>(brush);
            return solidBrush->GetOrCreateBrush(d2dContext_.Get());
        }
        case JALIUM_BRUSH_LINEAR_GRADIENT: {
            auto gradientBrush = static_cast<D3D12LinearGradientBrush*>(brush);
            return gradientBrush->GetOrCreateBrush(d2dContext_.Get());
        }
        default:
            return nullptr;
    }
}

void D3D12RenderTarget::FillRectangle(float x, float y, float w, float h, Brush* brush) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        d2dContext_->FillRectangle(D2D1::RectF(x, y, x + w, y + h), d2dBrush);
    }
}

void D3D12RenderTarget::DrawRectangle(float x, float y, float w, float h, Brush* brush, float strokeWidth) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        d2dContext_->DrawRectangle(D2D1::RectF(x, y, x + w, y + h), d2dBrush, strokeWidth);
    }
}

void D3D12RenderTarget::FillRoundedRectangle(float x, float y, float w, float h, float rx, float ry, Brush* brush) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        D2D1_ROUNDED_RECT roundedRect = D2D1::RoundedRect(D2D1::RectF(x, y, x + w, y + h), rx, ry);
        d2dContext_->FillRoundedRectangle(roundedRect, d2dBrush);
    }
}

void D3D12RenderTarget::DrawRoundedRectangle(float x, float y, float w, float h, float rx, float ry, Brush* brush, float strokeWidth) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        D2D1_ROUNDED_RECT roundedRect = D2D1::RoundedRect(D2D1::RectF(x, y, x + w, y + h), rx, ry);
        d2dContext_->DrawRoundedRectangle(roundedRect, d2dBrush, strokeWidth);
    }
}

void D3D12RenderTarget::FillEllipse(float cx, float cy, float rx, float ry, Brush* brush) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        d2dContext_->FillEllipse(D2D1::Ellipse(D2D1::Point2F(cx, cy), rx, ry), d2dBrush);
    }
}

void D3D12RenderTarget::DrawEllipse(float cx, float cy, float rx, float ry, Brush* brush, float strokeWidth) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        d2dContext_->DrawEllipse(D2D1::Ellipse(D2D1::Point2F(cx, cy), rx, ry), d2dBrush, strokeWidth);
    }
}

void D3D12RenderTarget::DrawLine(float x1, float y1, float x2, float y2, Brush* brush, float strokeWidth) {
    if (!isDrawing_ || !brush) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (d2dBrush) {
        d2dContext_->DrawLine(D2D1::Point2F(x1, y1), D2D1::Point2F(x2, y2), d2dBrush, strokeWidth);
    }
}

void D3D12RenderTarget::RenderText(
    const wchar_t* text, uint32_t textLength,
    TextFormat* format,
    float x, float y, float w, float h,
    Brush* brush)
{
    if (!isDrawing_ || !text || !format || !brush) return;

    // Use static_cast with format - it's always D3D12TextFormat when coming from this backend
    auto textFormat = static_cast<D3D12TextFormat*>(format);
    auto d2dBrush = GetD2DBrush(brush);

    if (textFormat && d2dBrush) {
        d2dContext_->DrawTextW(
            text, textLength,
            textFormat->GetFormat(),
            D2D1::RectF(x, y, x + w, y + h),
            d2dBrush);
    }
}

void D3D12RenderTarget::PushTransform(const float* matrix) {
    if (!matrix) return;

    D2D1_MATRIX_3X2_F transform = D2D1::Matrix3x2F(
        matrix[0], matrix[1],
        matrix[2], matrix[3],
        matrix[4], matrix[5]);

    D2D1_MATRIX_3X2_F current;
    d2dContext_->GetTransform(&current);

    D2D1_MATRIX_3X2_F combined = current * transform;
    d2dContext_->SetTransform(combined);

    transformStack_.push(combined);
}

void D3D12RenderTarget::PopTransform() {
    if (transformStack_.size() <= 1) return;

    transformStack_.pop();
    d2dContext_->SetTransform(transformStack_.top());
}

void D3D12RenderTarget::PushClip(float x, float y, float w, float h) {
    d2dContext_->PushAxisAlignedClip(
        D2D1::RectF(x, y, x + w, y + h),
        D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
    clipCount_++;
}

void D3D12RenderTarget::PopClip() {
    if (clipCount_ > 0) {
        d2dContext_->PopAxisAlignedClip();
        clipCount_--;
    }
}

void D3D12RenderTarget::PushOpacity(float opacity) {
    // D2D doesn't have a direct opacity stack, we'd need to use layers
    // For now, just track it
    opacityStack_.push(opacity);
}

void D3D12RenderTarget::PopOpacity() {
    if (!opacityStack_.empty()) {
        opacityStack_.pop();
    }
}

void D3D12RenderTarget::SetVSyncEnabled(bool enabled) {
    vsyncEnabled_ = enabled;
}

void D3D12RenderTarget::DrawBitmap(Bitmap* bitmap, float x, float y, float w, float h, float opacity) {
    if (!isDrawing_ || !bitmap) return;

    // Use static_cast - bitmap always comes from this backend
    auto d3d12Bitmap = static_cast<D3D12Bitmap*>(bitmap);
    auto d2dBitmap = d3d12Bitmap->GetOrCreateBitmap(d2dContext_.Get());
    if (!d2dBitmap) return;

    D2D1_RECT_F destRect = D2D1::RectF(x, y, x + w, y + h);
    d2dContext_->DrawBitmap(d2dBitmap, destRect, opacity);
}

void D3D12RenderTarget::DrawBackdropFilter(
    float x, float y, float w, float h,
    const char* backdropFilter,
    const char* material,
    const char* materialTint,
    float tintOpacity,
    float blurRadius,
    float cornerRadiusTL, float cornerRadiusTR,
    float cornerRadiusBR, float cornerRadiusBL)
{
    if (!isDrawing_) return;
    if (w <= 0 || h <= 0) return;

    // Parse tint color from hex string (e.g., "#RRGGBB")
    float r = 1.0f, g = 1.0f, b = 1.0f;
    if (materialTint && materialTint[0] == '#' && strlen(materialTint) >= 7) {
        int red = 0, green = 0, blue = 0;
        sscanf_s(materialTint + 1, "%02x%02x%02x", &red, &green, &blue);
        r = red / 255.0f;
        g = green / 255.0f;
        b = blue / 255.0f;
    }

    float avgRadius = (cornerRadiusTL + cornerRadiusTR + cornerRadiusBR + cornerRadiusBL) / 4.0f;
    D2D1_RECT_F destRect = D2D1::RectF(x, y, x + w, y + h);

    // Apply blur effect if blurRadius > 0
    if (blurRadius > 0.5f) {
        // Capture current render target content to snapshot
        if (CaptureSnapshot() && snapshotBitmaps_[frameIndex_]) {
            // Create Gaussian blur effect
            ComPtr<ID2D1Effect> blurEffect;
            HRESULT hr = d2dContext_->CreateEffect(CLSID_D2D1GaussianBlur, &blurEffect);

            if (SUCCEEDED(hr) && blurEffect) {
                // Use the snapshot bitmap as input
                blurEffect->SetInput(0, snapshotBitmaps_[frameIndex_].Get());
                blurEffect->SetValue(D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION, blurRadius / 3.0f);
                blurEffect->SetValue(D2D1_GAUSSIANBLUR_PROP_BORDER_MODE, D2D1_BORDER_MODE_HARD);

                // Set up clip for rounded corners if needed
                bool pushedLayer = false;
                if (avgRadius > 0.5f) {
                    ComPtr<ID2D1RoundedRectangleGeometry> clipGeom;
                    auto factory = backend_->GetD2DFactory();
                    D2D1_ROUNDED_RECT roundedRect = D2D1::RoundedRect(destRect, avgRadius, avgRadius);
                    hr = factory->CreateRoundedRectangleGeometry(roundedRect, &clipGeom);
                    if (SUCCEEDED(hr) && clipGeom) {
                        d2dContext_->PushLayer(
                            D2D1::LayerParameters(D2D1::InfiniteRect(), clipGeom.Get()),
                            nullptr);
                        pushedLayer = true;
                    }
                }

                // Draw blurred image at the destination rect
                D2D1_RECT_F sourceRect = D2D1::RectF(x, y, x + w, y + h);
                D2D1_POINT_2F drawPoint = D2D1::Point2F(x, y);
                d2dContext_->DrawImage(
                    blurEffect.Get(),
                    drawPoint,
                    sourceRect,
                    D2D1_INTERPOLATION_MODE_LINEAR,
                    D2D1_COMPOSITE_MODE_SOURCE_OVER);

                // Pop layer if we pushed one
                if (pushedLayer) {
                    d2dContext_->PopLayer();
                }
            }
        }
    }

    // Apply tint overlay if tintOpacity > 0
    if (tintOpacity > 0.01f) {
        ComPtr<ID2D1SolidColorBrush> tintBrush;
        HRESULT hr = d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(r, g, b, tintOpacity),
            &tintBrush);

        if (SUCCEEDED(hr) && tintBrush) {
            if (avgRadius > 0.5f) {
                D2D1_ROUNDED_RECT roundedRect = D2D1::RoundedRect(destRect, avgRadius, avgRadius);
                d2dContext_->FillRoundedRectangle(roundedRect, tintBrush.Get());
            } else {
                d2dContext_->FillRectangle(destRect, tintBrush.Get());
            }
        }
    }
}

bool D3D12RenderTarget::CreateSnapshotResources() {
    auto d3d11Context = backend_->GetD3D11Context();
    if (!d3d11Context) return false;

    ComPtr<ID3D11Device> d3d11Device;
    d3d11Context->GetDevice(&d3d11Device);
    if (!d3d11Device) return false;

    // Create snapshot textures for each frame
    D3D11_TEXTURE2D_DESC texDesc = {};
    texDesc.Width = width_;
    texDesc.Height = height_;
    texDesc.MipLevels = 1;
    texDesc.ArraySize = 1;
    texDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    texDesc.SampleDesc.Count = 1;
    texDesc.SampleDesc.Quality = 0;
    texDesc.Usage = D3D11_USAGE_DEFAULT;
    texDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    texDesc.CPUAccessFlags = 0;
    texDesc.MiscFlags = 0;

    for (uint32_t i = 0; i < FrameCount; ++i) {
        HRESULT hr = d3d11Device->CreateTexture2D(&texDesc, nullptr, &snapshotTextures_[i]);
        if (FAILED(hr)) return false;

        // Create D2D bitmap from snapshot texture
        ComPtr<IDXGISurface> surface;
        hr = snapshotTextures_[i].As(&surface);
        if (FAILED(hr)) return false;

        D2D1_BITMAP_PROPERTIES1 bitmapProps = D2D1::BitmapProperties1(
            D2D1_BITMAP_OPTIONS_NONE,  // Can be used as effect input
            D2D1::PixelFormat(DXGI_FORMAT_R8G8B8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED),
            96.0f, 96.0f);

        hr = d2dContext_->CreateBitmapFromDxgiSurface(surface.Get(), &bitmapProps, &snapshotBitmaps_[i]);
        if (FAILED(hr)) return false;

        snapshotValid_[i] = false;
    }

    return true;
}

bool D3D12RenderTarget::CaptureSnapshot() {
    auto d3d11Context = backend_->GetD3D11Context();
    if (!d3d11Context) return false;

    if (!snapshotTextures_[frameIndex_] || !wrappedBackBuffers_[frameIndex_]) return false;

    // Flush D2D to ensure all drawing is complete
    d2dContext_->Flush();

    // Get the back buffer as a D3D11 texture
    ComPtr<ID3D11Texture2D> backBufferTexture;
    HRESULT hr = wrappedBackBuffers_[frameIndex_].As(&backBufferTexture);
    if (FAILED(hr)) return false;

    // Copy the back buffer to the snapshot texture
    d3d11Context->CopyResource(snapshotTextures_[frameIndex_].Get(), backBufferTexture.Get());

    snapshotValid_[frameIndex_] = true;
    return true;
}

} // namespace jalium
