#include "d3d12_render_target.h"
#include "d3d12_resources.h"
#include "d3dx12.h"
#include <cstring>
#include <cstdio>
#include <algorithm>
#include <vector>

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

    // Use the factory from backend - DXGI requires using the same factory that created the device
    auto factory = backend_->GetDXGIFactory();
    if (!factory) return false;

    // Check for tearing support
    BOOL allowTearing = FALSE;
    ComPtr<IDXGIFactory5> factory5;
    if (SUCCEEDED(factory->QueryInterface(IID_PPV_ARGS(&factory5)))) {
        factory5->CheckFeatureSupport(DXGI_FEATURE_PRESENT_ALLOW_TEARING, &allowTearing, sizeof(allowTearing));
    }
    tearingSupported_ = (allowTearing == TRUE);

    HRESULT hr;

    // Create swap chain
    DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
    swapChainDesc.Width = width_;
    swapChainDesc.Height = height_;
    swapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    swapChainDesc.SampleDesc.Count = 1;
    swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    swapChainDesc.BufferCount = FrameCount;
    swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
    swapChainDesc.Scaling = DXGI_SCALING_STRETCH;
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

    // Use standard 96 DPI for DIPs - the managed code works in DIPs
    d2dContext_->SetDpi(96.0f, 96.0f);

    // Use default antialiasing modes
    d2dContext_->SetAntialiasMode(D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
    d2dContext_->SetTextAntialiasMode(D2D1_TEXT_ANTIALIAS_MODE_DEFAULT);

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
        // Coordinates are pre-rounded in managed code
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
        // Coordinates are pre-rounded in managed code
        d2dContext_->DrawLine(D2D1::Point2F(x1, y1), D2D1::Point2F(x2, y2), d2dBrush, strokeWidth);
    }
}

void D3D12RenderTarget::FillPolygon(const float* points, uint32_t pointCount, Brush* brush, int32_t fillRule) {
    if (!isDrawing_ || !brush || !points || pointCount < 3) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (!d2dBrush) return;

    auto factory = backend_->GetD2DFactory();
    if (!factory) return;

    ComPtr<ID2D1PathGeometry> pathGeometry;
    HRESULT hr = factory->CreatePathGeometry(&pathGeometry);
    if (FAILED(hr)) return;

    ComPtr<ID2D1GeometrySink> sink;
    hr = pathGeometry->Open(&sink);
    if (FAILED(hr)) return;

    // Set fill mode
    sink->SetFillMode(fillRule == 1 ? D2D1_FILL_MODE_WINDING : D2D1_FILL_MODE_ALTERNATE);

    // Begin figure at first point
    sink->BeginFigure(D2D1::Point2F(points[0], points[1]), D2D1_FIGURE_BEGIN_FILLED);

    // Add lines to remaining points
    for (uint32_t i = 1; i < pointCount; ++i) {
        sink->AddLine(D2D1::Point2F(points[i * 2], points[i * 2 + 1]));
    }

    // Close the figure
    sink->EndFigure(D2D1_FIGURE_END_CLOSED);
    hr = sink->Close();
    if (FAILED(hr)) return;

    // Fill the geometry
    d2dContext_->FillGeometry(pathGeometry.Get(), d2dBrush);
}

void D3D12RenderTarget::DrawPolygon(const float* points, uint32_t pointCount, Brush* brush, float strokeWidth, bool closed) {
    if (!isDrawing_ || !brush || !points || pointCount < 2) return;

    auto d2dBrush = GetD2DBrush(brush);
    if (!d2dBrush) return;

    auto factory = backend_->GetD2DFactory();
    if (!factory) return;

    ComPtr<ID2D1PathGeometry> pathGeometry;
    HRESULT hr = factory->CreatePathGeometry(&pathGeometry);
    if (FAILED(hr)) return;

    ComPtr<ID2D1GeometrySink> sink;
    hr = pathGeometry->Open(&sink);
    if (FAILED(hr)) return;

    // Begin figure at first point (hollow for stroke only)
    sink->BeginFigure(D2D1::Point2F(points[0], points[1]), D2D1_FIGURE_BEGIN_HOLLOW);

    // Add lines to remaining points
    for (uint32_t i = 1; i < pointCount; ++i) {
        sink->AddLine(D2D1::Point2F(points[i * 2], points[i * 2 + 1]));
    }

    // Close or leave open
    sink->EndFigure(closed ? D2D1_FIGURE_END_CLOSED : D2D1_FIGURE_END_OPEN);
    hr = sink->Close();
    if (FAILED(hr)) return;

    // Draw the geometry outline
    d2dContext_->DrawGeometry(pathGeometry.Get(), d2dBrush, strokeWidth);
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
    if (!isDrawing_ || !d2dContext_) return;

    // Use D2D layers to implement opacity
    // This correctly applies opacity to all drawing operations until PopOpacity
    d2dContext_->PushLayer(
        D2D1::LayerParameters(
            D2D1::InfiniteRect(),
            nullptr, // no clip geometry
            D2D1_ANTIALIAS_MODE_PER_PRIMITIVE,
            D2D1::IdentityMatrix(),
            opacity, // opacity value
            nullptr, // no opacity mask brush
            D2D1_LAYER_OPTIONS_NONE
        ),
        nullptr // let D2D manage the layer automatically
    );
    opacityStack_.push(opacity);
}

void D3D12RenderTarget::PopOpacity() {
    if (!isDrawing_ || !d2dContext_) return;

    if (!opacityStack_.empty()) {
        d2dContext_->PopLayer();
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

void D3D12RenderTarget::DrawGlowingBorderHighlight(
    float x, float y, float w, float h,
    float animationPhase,
    float glowColorR, float glowColorG, float glowColorB,
    float strokeWidth,
    float trailLength,
    float dimOpacity,
    float screenWidth, float screenHeight)
{
    if (!isDrawing_) return;
    if (w <= 0 || h <= 0) return;

    // Calculate the perimeter of the rectangle
    float perimeter = 2 * (w + h);

    // Trail length in pixels
    float trailLengthPx = perimeter * trailLength;

    // Current position along the perimeter (head of the trail)
    float headPos = animationPhase * perimeter;

    // Step 1: Draw dimmed overlay outside the highlighted area
    if (dimOpacity > 0.01f) {
        ComPtr<ID2D1SolidColorBrush> dimBrush;
        HRESULT hr = d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(0.0f, 0.0f, 0.0f, dimOpacity),
            &dimBrush);

        if (SUCCEEDED(hr) && dimBrush) {
            auto factory = backend_->GetD2DFactory();

            // Create outer rectangle (full screen)
            D2D1_RECT_F outerRect = D2D1::RectF(0, 0, screenWidth, screenHeight);
            // Create inner rectangle (highlighted area with expansion for spindle + glow)
            // Spindle maxWidth = strokeWidth * 2.5, plus glow layers up to strokeWidth * 4.5
            float glowExpand = strokeWidth * 10.0f;
            D2D1_RECT_F innerRect = D2D1::RectF(
                x - glowExpand, y - glowExpand,
                x + w + glowExpand, y + h + glowExpand);

            // Create geometries
            ComPtr<ID2D1RectangleGeometry> outerGeom, innerGeom;
            factory->CreateRectangleGeometry(outerRect, &outerGeom);
            factory->CreateRectangleGeometry(innerRect, &innerGeom);

            // Combine with exclude mode to create a frame around the element
            ComPtr<ID2D1PathGeometry> combinedGeom;
            factory->CreatePathGeometry(&combinedGeom);

            ComPtr<ID2D1GeometrySink> sink;
            if (SUCCEEDED(combinedGeom->Open(&sink))) {
                outerGeom->CombineWithGeometry(
                    innerGeom.Get(),
                    D2D1_COMBINE_MODE_EXCLUDE,
                    nullptr,
                    sink.Get());
                sink->Close();

                d2dContext_->FillGeometry(combinedGeom.Get(), dimBrush.Get());
            }
        }
    }

    // Step 2: Draw the glowing border trail as continuous path geometry
    auto factory = backend_->GetD2DFactory();

    // Create stroke style with round caps for smooth ends
    ComPtr<ID2D1StrokeStyle> roundStrokeStyle;
    D2D1_STROKE_STYLE_PROPERTIES strokeProps = D2D1::StrokeStyleProperties(
        D2D1_CAP_STYLE_ROUND,   // startCap
        D2D1_CAP_STYLE_ROUND,   // endCap
        D2D1_CAP_STYLE_ROUND,   // dashCap
        D2D1_LINE_JOIN_ROUND,   // lineJoin
        10.0f,                  // miterLimit
        D2D1_DASH_STYLE_SOLID,  // dashStyle
        0.0f                    // dashOffset
    );
    factory->CreateStrokeStyle(strokeProps, nullptr, 0, &roundStrokeStyle);

    // Lambda to convert perimeter position to x,y coordinates
    auto posToPoint = [x, y, w, h, perimeter](float pos) -> D2D1_POINT_2F {
        pos = fmodf(pos, perimeter);
        if (pos < 0) pos += perimeter;

        if (pos < w) {
            return D2D1::Point2F(x + pos, y);
        } else if (pos < w + h) {
            return D2D1::Point2F(x + w, y + (pos - w));
        } else if (pos < 2 * w + h) {
            return D2D1::Point2F(x + w - (pos - w - h), y + h);
        } else {
            return D2D1::Point2F(x, y + h - (pos - 2 * w - h));
        }
    };

    // Corner positions on perimeter (4 corners of the rectangle)
    float corners[] = { w, w + h, 2 * w + h };  // Top-right, bottom-right, bottom-left

    // Helper to find next corner after a given position
    auto getNextCorner = [&](float pos) -> float {
        pos = fmodf(pos, perimeter);
        if (pos < 0) pos += perimeter;

        if (pos < w) return w;                    // On top edge -> next is top-right corner
        if (pos < w + h) return w + h;            // On right edge -> next is bottom-right corner
        if (pos < 2 * w + h) return 2 * w + h;    // On bottom edge -> next is bottom-left corner
        return perimeter;                          // On left edge -> next is top-left (wrap to 0)
    };

    // Build continuous path geometry for the trail
    auto buildTrailPath = [&](float startPos, float length) -> ComPtr<ID2D1PathGeometry> {
        ComPtr<ID2D1PathGeometry> pathGeom;
        if (FAILED(factory->CreatePathGeometry(&pathGeom))) return nullptr;

        ComPtr<ID2D1GeometrySink> sink;
        if (FAILED(pathGeom->Open(&sink))) return nullptr;

        // Normalize start position
        float pos = fmodf(startPos, perimeter);
        if (pos < 0) pos += perimeter;

        sink->BeginFigure(posToPoint(pos), D2D1_FIGURE_BEGIN_HOLLOW);

        float traveled = 0.0f;
        while (traveled < length - 0.01f) {
            // Find the next corner from current position
            float nextCorner = getNextCorner(pos);
            float distToCorner = nextCorner - pos;
            if (distToCorner <= 0) distToCorner += perimeter;  // Handle wrap

            float remainingLength = length - traveled;
            float segLen = (distToCorner < remainingLength) ? distToCorner : remainingLength;

            // Calculate end position
            float endPos = pos + segLen;
            if (endPos >= perimeter) endPos -= perimeter;

            sink->AddLine(posToPoint(endPos));

            traveled += segLen;
            pos = endPos;
        }

        sink->EndFigure(D2D1_FIGURE_END_OPEN);
        sink->Close();
        return pathGeom;
    };

    // SHADER-BASED SPINDLE EFFECT
    // Step 1: Build a filled spindle geometry (variable width along the path)
    // Step 2: Draw to bitmap, apply blur effect for glow
    // Step 3: Composite with original

    float tailPos = headPos - trailLengthPx;
    if (tailPos < 0) tailPos += perimeter;

    // Build spindle outline geometry - create two parallel paths offset by variable width
    const int numPoints = 64;  // Smooth curve
    const float maxWidth = strokeWidth * 2.5f;  // Maximum spindle width at center

    ComPtr<ID2D1PathGeometry> spindleGeom;
    if (FAILED(factory->CreatePathGeometry(&spindleGeom))) return;

    ComPtr<ID2D1GeometrySink> sink;
    if (FAILED(spindleGeom->Open(&sink))) return;

    // Calculate points along the trail with spindle width
    std::vector<D2D1_POINT_2F> centerPoints;
    std::vector<float> widths;

    for (int i = 0; i <= numPoints; i++) {
        float t = (float)i / numPoints;  // 0 = tail, 1 = head
        float pos = tailPos + t * trailLengthPx;
        if (pos >= perimeter) pos -= perimeter;

        centerPoints.push_back(posToPoint(pos));

        // Spindle width: sin(π * t) - tapers to 0 at both ends, max in middle
        float spindleFactor = sinf(3.14159f * t);
        widths.push_back(maxWidth * spindleFactor);  // Pure sine: 0 at ends, 1 at middle
    }

    // Calculate normals and build outline
    auto getNormal = [](D2D1_POINT_2F p1, D2D1_POINT_2F p2) -> D2D1_POINT_2F {
        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        float len = sqrtf(dx * dx + dy * dy);
        if (len < 0.001f) return D2D1::Point2F(0, 1);
        return D2D1::Point2F(-dy / len, dx / len);
    };

    // Build the spindle shape as a filled polygon
    // First, go forward along one side, then backward along the other
    std::vector<D2D1_POINT_2F> outline;

    // Forward pass (one side)
    for (int i = 0; i <= numPoints; i++) {
        D2D1_POINT_2F normal;
        if (i == 0) {
            normal = getNormal(centerPoints[0], centerPoints[1]);
        } else if (i == numPoints) {
            normal = getNormal(centerPoints[numPoints - 1], centerPoints[numPoints]);
        } else {
            normal = getNormal(centerPoints[i - 1], centerPoints[i + 1]);
        }
        float halfWidth = widths[i] * 0.5f;
        outline.push_back(D2D1::Point2F(
            centerPoints[i].x + normal.x * halfWidth,
            centerPoints[i].y + normal.y * halfWidth));
    }

    // Backward pass (other side)
    for (int i = numPoints; i >= 0; i--) {
        D2D1_POINT_2F normal;
        if (i == 0) {
            normal = getNormal(centerPoints[0], centerPoints[1]);
        } else if (i == numPoints) {
            normal = getNormal(centerPoints[numPoints - 1], centerPoints[numPoints]);
        } else {
            normal = getNormal(centerPoints[i - 1], centerPoints[i + 1]);
        }
        float halfWidth = widths[i] * 0.5f;
        outline.push_back(D2D1::Point2F(
            centerPoints[i].x - normal.x * halfWidth,
            centerPoints[i].y - normal.y * halfWidth));
    }

    // Create the geometry
    if (!outline.empty()) {
        sink->BeginFigure(outline[0], D2D1_FIGURE_BEGIN_FILLED);
        for (size_t i = 1; i < outline.size(); i++) {
            sink->AddLine(outline[i]);
        }
        sink->EndFigure(D2D1_FIGURE_END_CLOSED);
    }
    sink->Close();

    // Create brush for the spindle
    ComPtr<ID2D1SolidColorBrush> spindleBrush;
    d2dContext_->CreateSolidColorBrush(
        D2D1::ColorF(glowColorR, glowColorG, glowColorB, 0.9f),
        &spindleBrush);

    if (!spindleBrush) return;

    // Create clip geometry to exclude inner area (glow only outside the element)
    ComPtr<ID2D1RectangleGeometry> glowOuterGeom, glowInnerGeom;
    float maxGlowSize = strokeWidth * 4.5f;
    factory->CreateRectangleGeometry(
        D2D1::RectF(x - maxGlowSize, y - maxGlowSize,
                    x + w + maxGlowSize, y + h + maxGlowSize),
        &glowOuterGeom);
    factory->CreateRectangleGeometry(
        D2D1::RectF(x, y, x + w, y + h),
        &glowInnerGeom);

    ComPtr<ID2D1PathGeometry> glowClipGeom;
    factory->CreatePathGeometry(&glowClipGeom);
    ComPtr<ID2D1GeometrySink> glowClipSink;
    bool hasGlowClip = false;
    if (SUCCEEDED(glowClipGeom->Open(&glowClipSink))) {
        glowOuterGeom->CombineWithGeometry(glowInnerGeom.Get(), D2D1_COMBINE_MODE_EXCLUDE, nullptr, glowClipSink.Get());
        glowClipSink->Close();
        hasGlowClip = true;
    }

    // Draw outer glow layers (multiple passes with increasing blur simulation)
    // Use clip layer to only show glow OUTSIDE the element (not inside)
    if (hasGlowClip) {
        ComPtr<ID2D1Layer> glowClipLayer;
        d2dContext_->CreateLayer(&glowClipLayer);
        d2dContext_->PushLayer(
            D2D1::LayerParameters(D2D1::InfiniteRect(), glowClipGeom.Get()),
            glowClipLayer.Get());
    }

    for (int glowLayer = 3; glowLayer >= 0; glowLayer--) {
        float glowScale = 1.0f + glowLayer * 0.4f;
        float glowOpacity = 0.9f * powf(0.5f, (float)glowLayer);

        // Scale the geometry for glow effect by drawing with different stroke widths
        ComPtr<ID2D1SolidColorBrush> glowBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(glowColorR, glowColorG, glowColorB, glowOpacity),
            &glowBrush);

        if (glowBrush && glowLayer > 0) {
            // Draw stroked outline for outer glow
            d2dContext_->DrawGeometry(spindleGeom.Get(), glowBrush.Get(),
                strokeWidth * glowLayer * 1.5f, roundStrokeStyle.Get());
        }
    }

    // Draw the core spindle shape (also clipped to outside only)
    d2dContext_->FillGeometry(spindleGeom.Get(), spindleBrush.Get());

    if (hasGlowClip) {
        d2dContext_->PopLayer();
    }

    // Step 3: Draw a subtle static border for reference
    ComPtr<ID2D1SolidColorBrush> borderBrush;
    HRESULT hr = d2dContext_->CreateSolidColorBrush(
        D2D1::ColorF(glowColorR, glowColorG, glowColorB, 0.3f),
        &borderBrush);

    if (SUCCEEDED(hr) && borderBrush) {
        d2dContext_->DrawRectangle(D2D1::RectF(x, y, x + w, y + h), borderBrush.Get(), 1.0f);
    }
}

void D3D12RenderTarget::DrawGlowingBorderTransition(
    float fromX, float fromY, float fromW, float fromH,
    float toX, float toY, float toW, float toH,
    float headProgress, float tailProgress,
    float animationPhase,
    float glowColorR, float glowColorG, float glowColorB,
    float strokeWidth,
    float trailLength,
    float dimOpacity,
    float screenWidth, float screenHeight)
{
    if (!d2dContext_) return;

    auto factory = backend_->GetD2DFactory();
    const float maxWidth = strokeWidth * 2.5f;

    // Calculate center points of source and target rectangles
    float fromCenterX = fromX + fromW / 2;
    float fromCenterY = fromY + fromH / 2;
    float toCenterX = toX + toW / 2;
    float toCenterY = toY + toH / 2;

    // Head position: interpolates from source border to target border
    // Tail position: follows behind the head
    auto lerp = [](float a, float b, float t) { return a + (b - a) * t; };

    // Calculate head and tail positions along the transition path
    float headX = lerp(fromCenterX, toCenterX, headProgress);
    float headY = lerp(fromCenterY, toCenterY, headProgress);
    float tailX = lerp(fromCenterX, toCenterX, tailProgress);
    float tailY = lerp(fromCenterY, toCenterY, tailProgress);

    // Draw dimmed overlay - blend between source and target
    if (dimOpacity > 0.01f) {
        ComPtr<ID2D1SolidColorBrush> dimBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(0.0f, 0.0f, 0.0f, dimOpacity),
            &dimBrush);

        if (dimBrush) {
            // Interpolate the highlighted area
            float highlightX = lerp(fromX, toX, headProgress);
            float highlightY = lerp(fromY, toY, headProgress);
            float highlightW = lerp(fromW, toW, headProgress);
            float highlightH = lerp(fromH, toH, headProgress);

            float glowExpand = strokeWidth * 10.0f;
            D2D1_RECT_F outerRect = D2D1::RectF(0, 0, screenWidth, screenHeight);
            D2D1_RECT_F innerRect = D2D1::RectF(
                highlightX - glowExpand, highlightY - glowExpand,
                highlightX + highlightW + glowExpand, highlightY + highlightH + glowExpand);

            ComPtr<ID2D1RectangleGeometry> outerGeom, innerGeom;
            factory->CreateRectangleGeometry(outerRect, &outerGeom);
            factory->CreateRectangleGeometry(innerRect, &innerGeom);

            ComPtr<ID2D1PathGeometry> combinedGeom;
            factory->CreatePathGeometry(&combinedGeom);

            ComPtr<ID2D1GeometrySink> sink;
            if (SUCCEEDED(combinedGeom->Open(&sink))) {
                outerGeom->CombineWithGeometry(innerGeom.Get(), D2D1_COMBINE_MODE_EXCLUDE, nullptr, sink.Get());
                sink->Close();
                d2dContext_->FillGeometry(combinedGeom.Get(), dimBrush.Get());
            }
        }
    }

    // Create stroke style
    ComPtr<ID2D1StrokeStyle> roundStrokeStyle;
    D2D1_STROKE_STYLE_PROPERTIES strokeProps = D2D1::StrokeStyleProperties(
        D2D1_CAP_STYLE_ROUND, D2D1_CAP_STYLE_ROUND, D2D1_CAP_STYLE_ROUND,
        D2D1_LINE_JOIN_ROUND, 10.0f, D2D1_DASH_STYLE_SOLID, 0.0f);
    factory->CreateStrokeStyle(strokeProps, nullptr, 0, &roundStrokeStyle);

    // Build spindle geometry between tail and head
    const int numPoints = 32;
    std::vector<D2D1_POINT_2F> centerPoints;
    std::vector<float> widths;

    // Create points along the path from tail to head
    for (int i = 0; i <= numPoints; i++) {
        float t = (float)i / numPoints;

        // Position along the transition path (from tail to head)
        float posX = lerp(tailX, headX, t);
        float posY = lerp(tailY, headY, t);
        centerPoints.push_back(D2D1::Point2F(posX, posY));

        // Spindle width: sin(π * t)
        float spindleFactor = sinf(3.14159f * t);
        widths.push_back(maxWidth * spindleFactor);
    }

    // Build the spindle outline
    auto getNormal = [](D2D1_POINT_2F p1, D2D1_POINT_2F p2) -> D2D1_POINT_2F {
        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        float len = sqrtf(dx * dx + dy * dy);
        if (len < 0.001f) return D2D1::Point2F(0, 1);
        return D2D1::Point2F(-dy / len, dx / len);
    };

    std::vector<D2D1_POINT_2F> outline;

    // Forward pass
    for (int i = 0; i <= numPoints; i++) {
        D2D1_POINT_2F normal;
        if (i == 0) normal = getNormal(centerPoints[0], centerPoints[1]);
        else if (i == numPoints) normal = getNormal(centerPoints[numPoints - 1], centerPoints[numPoints]);
        else normal = getNormal(centerPoints[i - 1], centerPoints[i + 1]);

        float halfWidth = widths[i] * 0.5f;
        outline.push_back(D2D1::Point2F(centerPoints[i].x + normal.x * halfWidth, centerPoints[i].y + normal.y * halfWidth));
    }

    // Backward pass
    for (int i = numPoints; i >= 0; i--) {
        D2D1_POINT_2F normal;
        if (i == 0) normal = getNormal(centerPoints[0], centerPoints[1]);
        else if (i == numPoints) normal = getNormal(centerPoints[numPoints - 1], centerPoints[numPoints]);
        else normal = getNormal(centerPoints[i - 1], centerPoints[i + 1]);

        float halfWidth = widths[i] * 0.5f;
        outline.push_back(D2D1::Point2F(centerPoints[i].x - normal.x * halfWidth, centerPoints[i].y - normal.y * halfWidth));
    }

    // Create spindle geometry
    ComPtr<ID2D1PathGeometry> spindleGeom;
    factory->CreatePathGeometry(&spindleGeom);

    ComPtr<ID2D1GeometrySink> spindleSink;
    if (SUCCEEDED(spindleGeom->Open(&spindleSink)) && !outline.empty()) {
        spindleSink->BeginFigure(outline[0], D2D1_FIGURE_BEGIN_FILLED);
        for (size_t i = 1; i < outline.size(); i++) {
            spindleSink->AddLine(outline[i]);
        }
        spindleSink->EndFigure(D2D1_FIGURE_END_CLOSED);
        spindleSink->Close();

        // Draw glow layers
        for (int glowLayer = 3; glowLayer >= 0; glowLayer--) {
            float glowOpacity = 0.9f * powf(0.5f, (float)glowLayer);

            ComPtr<ID2D1SolidColorBrush> glowBrush;
            d2dContext_->CreateSolidColorBrush(
                D2D1::ColorF(glowColorR, glowColorG, glowColorB, glowOpacity),
                &glowBrush);

            if (glowBrush && glowLayer > 0) {
                d2dContext_->DrawGeometry(spindleGeom.Get(), glowBrush.Get(),
                    strokeWidth * glowLayer * 1.5f, roundStrokeStyle.Get());
            }
        }

        // Draw core
        ComPtr<ID2D1SolidColorBrush> spindleBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(glowColorR, glowColorG, glowColorB, 0.9f),
            &spindleBrush);

        if (spindleBrush) {
            d2dContext_->FillGeometry(spindleGeom.Get(), spindleBrush.Get());
        }
    }

    // Draw target element border (fades in)
    ComPtr<ID2D1SolidColorBrush> borderBrush;
    d2dContext_->CreateSolidColorBrush(
        D2D1::ColorF(glowColorR, glowColorG, glowColorB, 0.3f * headProgress),
        &borderBrush);

    if (borderBrush) {
        d2dContext_->DrawRectangle(D2D1::RectF(toX, toY, toX + toW, toY + toH), borderBrush.Get(), 1.0f);
    }
}

void D3D12RenderTarget::DrawRippleEffect(
    float x, float y, float w, float h,
    float rippleProgress,
    float glowColorR, float glowColorG, float glowColorB,
    float strokeWidth,
    float dimOpacity,
    float screenWidth, float screenHeight)
{
    if (!d2dContext_) return;

    auto factory = backend_->GetD2DFactory();

    // Calculate element center
    float centerX = x + w / 2;
    float centerY = y + h / 2;

    // Number of ripple rings
    const int numRipples = 3;

    // Draw dimmed overlay with hole at element position
    if (dimOpacity > 0.01f) {
        ComPtr<ID2D1SolidColorBrush> dimBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(0.0f, 0.0f, 0.0f, dimOpacity),
            &dimBrush);

        if (dimBrush) {
            float glowExpand = strokeWidth * 10.0f;
            D2D1_RECT_F outerRect = D2D1::RectF(0, 0, screenWidth, screenHeight);
            D2D1_RECT_F innerRect = D2D1::RectF(
                x - glowExpand, y - glowExpand,
                x + w + glowExpand, y + h + glowExpand);

            ComPtr<ID2D1RectangleGeometry> outerGeom, innerGeom;
            factory->CreateRectangleGeometry(outerRect, &outerGeom);
            factory->CreateRectangleGeometry(innerRect, &innerGeom);

            ComPtr<ID2D1PathGeometry> combinedGeom;
            factory->CreatePathGeometry(&combinedGeom);

            ComPtr<ID2D1GeometrySink> sink;
            if (SUCCEEDED(combinedGeom->Open(&sink))) {
                outerGeom->CombineWithGeometry(innerGeom.Get(), D2D1_COMBINE_MODE_EXCLUDE, nullptr, sink.Get());
                sink->Close();
                d2dContext_->FillGeometry(combinedGeom.Get(), dimBrush.Get());
            }
        }
    }

    // Glow size based on element height (larger elements get larger glow)
    float baseGlowSize = max(h * 0.3f, 20.0f);  // At least 20px, or 30% of height

    // Draw multiple ripple rings emanating from center, expanding to element border (rectangle shape)
    // With inner glow only (glow toward center, not outward)
    for (int i = 0; i < numRipples; i++) {
        // Each ripple is offset in time (staggered start)
        float rippleOffset = (float)i / numRipples;
        float adjustedProgress = rippleProgress - rippleOffset * 0.3f;

        if (adjustedProgress < 0.0f) continue;
        adjustedProgress = min(1.0f, adjustedProgress / (1.0f - rippleOffset * 0.3f));

        // Calculate ripple size (from center, expands to element border)
        float currentW = adjustedProgress * w;
        float currentH = adjustedProgress * h;

        // Opacity fades out as ripple expands (faster fade for outer ripples)
        float fadeSpeed = 1.0f + i * 0.5f;
        float opacity = (1.0f - powf(adjustedProgress, fadeSpeed)) * 0.9f;

        if (opacity < 0.01f) continue;

        // Draw rectangle ripple from center
        float rippleX = centerX - currentW / 2;
        float rippleY = centerY - currentH / 2;

        // Small corner radius for subtle rounding
        float cornerRadius = min(currentW, currentH) * 0.05f;

        // Glow stroke width based on element height, decreases as ripple expands
        float glowStrokeWidth = baseGlowSize * (1.0f - adjustedProgress * 0.5f);
        glowStrokeWidth = max(strokeWidth * 2.0f, glowStrokeWidth);

        D2D1_ROUNDED_RECT roundedRect = D2D1::RoundedRect(
            D2D1::RectF(rippleX, rippleY, rippleX + currentW, rippleY + currentH),
            cornerRadius, cornerRadius);

        // Create clip geometry for inner glow only (clip to inside of ripple rectangle)
        ComPtr<ID2D1RectangleGeometry> innerClipGeom;
        factory->CreateRectangleGeometry(
            D2D1::RectF(rippleX, rippleY, rippleX + currentW, rippleY + currentH),
            &innerClipGeom);

        // Push layer with inner clip
        D2D1_LAYER_PARAMETERS layerParams = D2D1::LayerParameters(
            D2D1::InfiniteRect(),
            innerClipGeom.Get());
        d2dContext_->PushLayer(layerParams, nullptr);

        // Draw multiple glow layers for softer effect (inner glow only due to clip)
        // Outer glow layer (softest, largest)
        ComPtr<ID2D1SolidColorBrush> outerGlowBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(glowColorR, glowColorG, glowColorB, opacity * 0.15f),
            &outerGlowBrush);
        if (outerGlowBrush) {
            d2dContext_->DrawRoundedRectangle(roundedRect, outerGlowBrush.Get(), glowStrokeWidth * 2.0f);
        }

        // Middle glow layer
        ComPtr<ID2D1SolidColorBrush> midGlowBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(glowColorR, glowColorG, glowColorB, opacity * 0.3f),
            &midGlowBrush);
        if (midGlowBrush) {
            d2dContext_->DrawRoundedRectangle(roundedRect, midGlowBrush.Get(), glowStrokeWidth);
        }

        // Inner glow layer (brighter)
        ComPtr<ID2D1SolidColorBrush> innerGlowBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(glowColorR, glowColorG, glowColorB, opacity * 0.6f),
            &innerGlowBrush);
        if (innerGlowBrush) {
            d2dContext_->DrawRoundedRectangle(roundedRect, innerGlowBrush.Get(), glowStrokeWidth * 0.5f);
        }

        // Draw the main ripple stroke (brightest core)
        ComPtr<ID2D1SolidColorBrush> rippleBrush;
        d2dContext_->CreateSolidColorBrush(
            D2D1::ColorF(glowColorR, glowColorG, glowColorB, opacity),
            &rippleBrush);

        if (rippleBrush) {
            float coreStrokeWidth = strokeWidth * 1.5f * (1.0f - adjustedProgress * 0.5f);
            d2dContext_->DrawRoundedRectangle(roundedRect, rippleBrush.Get(), max(1.0f, coreStrokeWidth));
        }

        // Pop the inner clip layer
        d2dContext_->PopLayer();
    }

    // Draw the element border (fades in as ripple reaches edge)
    ComPtr<ID2D1SolidColorBrush> borderBrush;
    float borderOpacity = 0.6f + 0.4f * rippleProgress; // Fades in to full
    d2dContext_->CreateSolidColorBrush(
        D2D1::ColorF(glowColorR, glowColorG, glowColorB, borderOpacity),
        &borderBrush);

    if (borderBrush) {
        d2dContext_->DrawRectangle(D2D1::RectF(x, y, x + w, y + h), borderBrush.Get(), 1.0f);
    }
}

} // namespace jalium
