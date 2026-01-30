#include "d3d12_resources.h"
#include "d3d12_backend.h"

namespace jalium {

D3D12Bitmap::D3D12Bitmap(uint32_t width, uint32_t height)
    : width_(width)
    , height_(height)
{
}

void D3D12Bitmap::SetBitmapData(const uint8_t* data, uint32_t dataSize) {
    pixelData_.assign(data, data + dataSize);
}

ID2D1Bitmap* D3D12Bitmap::GetOrCreateBitmap(ID2D1DeviceContext* context) {
    if (!context) return nullptr;

    // If we already have a bitmap for this context, return it
    if (bitmap_ && lastContext_ == context) {
        return bitmap_.Get();
    }

    // Need to create the bitmap
    if (pixelData_.empty() || width_ == 0 || height_ == 0) {
        return nullptr;
    }

    // Create D2D bitmap from pixel data (BGRA format)
    D2D1_BITMAP_PROPERTIES bitmapProperties = D2D1::BitmapProperties(
        D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED)
    );

    bitmap_.Reset();
    HRESULT hr = context->CreateBitmap(
        D2D1::SizeU(width_, height_),
        pixelData_.data(),
        width_ * 4,  // stride: 4 bytes per pixel (BGRA)
        bitmapProperties,
        &bitmap_
    );

    if (FAILED(hr)) {
        return nullptr;
    }

    lastContext_ = context;
    return bitmap_.Get();
}

} // namespace jalium
