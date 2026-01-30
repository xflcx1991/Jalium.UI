// Minimal d3dx12.h helper - subset of the official D3DX12 header
// Full version available from: https://github.com/microsoft/DirectX-Headers

#pragma once

#include <d3d12.h>

// CD3DX12_CPU_DESCRIPTOR_HANDLE
struct CD3DX12_CPU_DESCRIPTOR_HANDLE : public D3D12_CPU_DESCRIPTOR_HANDLE {
    CD3DX12_CPU_DESCRIPTOR_HANDLE() = default;

    explicit CD3DX12_CPU_DESCRIPTOR_HANDLE(const D3D12_CPU_DESCRIPTOR_HANDLE& o) noexcept
        : D3D12_CPU_DESCRIPTOR_HANDLE(o) {}

    CD3DX12_CPU_DESCRIPTOR_HANDLE(D3D12_CPU_DESCRIPTOR_HANDLE other, INT offsetScaledByIncrementSize) noexcept {
        InitOffsetted(other, offsetScaledByIncrementSize);
    }

    CD3DX12_CPU_DESCRIPTOR_HANDLE(D3D12_CPU_DESCRIPTOR_HANDLE other, INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        InitOffsetted(other, offsetInDescriptors, descriptorIncrementSize);
    }

    CD3DX12_CPU_DESCRIPTOR_HANDLE& Offset(INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        ptr = SIZE_T(INT64(ptr) + INT64(offsetInDescriptors) * INT64(descriptorIncrementSize));
        return *this;
    }

    CD3DX12_CPU_DESCRIPTOR_HANDLE& Offset(INT offsetScaledByIncrementSize) noexcept {
        ptr = SIZE_T(INT64(ptr) + INT64(offsetScaledByIncrementSize));
        return *this;
    }

    void InitOffsetted(D3D12_CPU_DESCRIPTOR_HANDLE base, INT offsetScaledByIncrementSize) noexcept {
        InitOffsetted(*this, base, offsetScaledByIncrementSize);
    }

    void InitOffsetted(D3D12_CPU_DESCRIPTOR_HANDLE base, INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        InitOffsetted(*this, base, offsetInDescriptors, descriptorIncrementSize);
    }

    static void InitOffsetted(D3D12_CPU_DESCRIPTOR_HANDLE& handle, D3D12_CPU_DESCRIPTOR_HANDLE base, INT offsetScaledByIncrementSize) noexcept {
        handle.ptr = SIZE_T(INT64(base.ptr) + INT64(offsetScaledByIncrementSize));
    }

    static void InitOffsetted(D3D12_CPU_DESCRIPTOR_HANDLE& handle, D3D12_CPU_DESCRIPTOR_HANDLE base, INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        handle.ptr = SIZE_T(INT64(base.ptr) + INT64(offsetInDescriptors) * INT64(descriptorIncrementSize));
    }
};

// CD3DX12_GPU_DESCRIPTOR_HANDLE
struct CD3DX12_GPU_DESCRIPTOR_HANDLE : public D3D12_GPU_DESCRIPTOR_HANDLE {
    CD3DX12_GPU_DESCRIPTOR_HANDLE() = default;

    explicit CD3DX12_GPU_DESCRIPTOR_HANDLE(const D3D12_GPU_DESCRIPTOR_HANDLE& o) noexcept
        : D3D12_GPU_DESCRIPTOR_HANDLE(o) {}

    CD3DX12_GPU_DESCRIPTOR_HANDLE(D3D12_GPU_DESCRIPTOR_HANDLE other, INT offsetScaledByIncrementSize) noexcept {
        InitOffsetted(other, offsetScaledByIncrementSize);
    }

    CD3DX12_GPU_DESCRIPTOR_HANDLE(D3D12_GPU_DESCRIPTOR_HANDLE other, INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        InitOffsetted(other, offsetInDescriptors, descriptorIncrementSize);
    }

    CD3DX12_GPU_DESCRIPTOR_HANDLE& Offset(INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        ptr = UINT64(INT64(ptr) + INT64(offsetInDescriptors) * INT64(descriptorIncrementSize));
        return *this;
    }

    CD3DX12_GPU_DESCRIPTOR_HANDLE& Offset(INT offsetScaledByIncrementSize) noexcept {
        ptr = UINT64(INT64(ptr) + INT64(offsetScaledByIncrementSize));
        return *this;
    }

    void InitOffsetted(D3D12_GPU_DESCRIPTOR_HANDLE base, INT offsetScaledByIncrementSize) noexcept {
        InitOffsetted(*this, base, offsetScaledByIncrementSize);
    }

    void InitOffsetted(D3D12_GPU_DESCRIPTOR_HANDLE base, INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        InitOffsetted(*this, base, offsetInDescriptors, descriptorIncrementSize);
    }

    static void InitOffsetted(D3D12_GPU_DESCRIPTOR_HANDLE& handle, D3D12_GPU_DESCRIPTOR_HANDLE base, INT offsetScaledByIncrementSize) noexcept {
        handle.ptr = UINT64(INT64(base.ptr) + INT64(offsetScaledByIncrementSize));
    }

    static void InitOffsetted(D3D12_GPU_DESCRIPTOR_HANDLE& handle, D3D12_GPU_DESCRIPTOR_HANDLE base, INT offsetInDescriptors, UINT descriptorIncrementSize) noexcept {
        handle.ptr = UINT64(INT64(base.ptr) + INT64(offsetInDescriptors) * INT64(descriptorIncrementSize));
    }
};
