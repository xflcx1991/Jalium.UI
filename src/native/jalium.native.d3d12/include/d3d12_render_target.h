#pragma once

#include "jalium_backend.h"
#include "d3d12_backend.h"
#include <stack>
#include <vector>

namespace jalium {

/// D3D12 render target implementation.
/// Uses D2D1 DeviceContext for 2D rendering on a D3D12 swap chain.
class D3D12RenderTarget : public RenderTarget {
public:
    D3D12RenderTarget(D3D12Backend* backend, void* hwnd, int32_t width, int32_t height);
    ~D3D12RenderTarget() override;

    /// Initializes the render target.
    bool Initialize();

    // RenderTarget implementation
    JaliumResult Resize(int32_t width, int32_t height) override;
    JaliumResult BeginDraw() override;
    JaliumResult EndDraw() override;
    void Clear(float r, float g, float b, float a) override;

    void FillRectangle(float x, float y, float w, float h, Brush* brush) override;
    void DrawRectangle(float x, float y, float w, float h, Brush* brush, float strokeWidth) override;
    void FillRoundedRectangle(float x, float y, float w, float h, float rx, float ry, Brush* brush) override;
    void DrawRoundedRectangle(float x, float y, float w, float h, float rx, float ry, Brush* brush, float strokeWidth) override;
    void FillEllipse(float cx, float cy, float rx, float ry, Brush* brush) override;
    void DrawEllipse(float cx, float cy, float rx, float ry, Brush* brush, float strokeWidth) override;
    void DrawLine(float x1, float y1, float x2, float y2, Brush* brush, float strokeWidth) override;
    void RenderText(
        const wchar_t* text, uint32_t textLength,
        TextFormat* format,
        float x, float y, float w, float h,
        Brush* brush) override;

    void PushTransform(const float* matrix) override;
    void PopTransform() override;
    void PushClip(float x, float y, float w, float h) override;
    void PopClip() override;
    void PushOpacity(float opacity) override;
    void PopOpacity() override;
    void SetVSyncEnabled(bool enabled) override;
    void DrawBitmap(Bitmap* bitmap, float x, float y, float w, float h, float opacity) override;
    void DrawBackdropFilter(
        float x, float y, float w, float h,
        const char* backdropFilter,
        const char* material,
        const char* materialTint,
        float tintOpacity,
        float blurRadius,
        float cornerRadiusTL, float cornerRadiusTR,
        float cornerRadiusBR, float cornerRadiusBL) override;

private:
    static constexpr uint32_t FrameCount = 2;

    bool CreateSwapChain();
    bool CreateRenderTargetViews();
    bool CreateD2DRenderTarget();
    void WaitForGpu();
    void WaitForAllFrames();
    void MoveToNextFrame();

    // Helper to get D2D brush without using dynamic_cast (avoids cross-DLL RTTI issues)
    ID2D1Brush* GetD2DBrush(Brush* brush);

    D3D12Backend* backend_;
    HWND hwnd_;

    // Swap chain resources
    ComPtr<IDXGISwapChain3> swapChain_;
    ComPtr<ID3D12DescriptorHeap> rtvHeap_;
    uint32_t rtvDescriptorSize_ = 0;
    uint32_t frameIndex_ = 0;

    // Per-frame resources
    ComPtr<ID3D12Resource> renderTargets_[FrameCount];
    ComPtr<ID3D12CommandAllocator> commandAllocators_[FrameCount];
    ComPtr<ID3D11Resource> wrappedBackBuffers_[FrameCount];
    ComPtr<ID2D1Bitmap1> d2dRenderTargets_[FrameCount];

    // Command list
    ComPtr<ID3D12GraphicsCommandList> commandList_;

    // Synchronization
    ComPtr<ID3D12Fence> fence_;
    uint64_t fenceValues_[FrameCount] = {};
    HANDLE fenceEvent_ = nullptr;

    // D2D device context for drawing
    ComPtr<ID2D1DeviceContext2> d2dContext_;

    // State stacks
    std::stack<D2D1_MATRIX_3X2_F> transformStack_;
    std::stack<float> opacityStack_;
    uint32_t clipCount_ = 0;

    bool isDrawing_ = false;
    bool tearingSupported_ = false;

    // Backdrop blur resources - per-frame snapshot textures
    ComPtr<ID3D11Texture2D> snapshotTextures_[FrameCount];
    ComPtr<ID2D1Bitmap1> snapshotBitmaps_[FrameCount];
    bool snapshotValid_[FrameCount] = { false, false };

    // Helper to capture current render target for backdrop blur
    bool CaptureSnapshot();
    bool CreateSnapshotResources();
};

} // namespace jalium
