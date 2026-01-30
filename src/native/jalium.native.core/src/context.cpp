#include "jalium_internal.h"

// ============================================================================
// C API Implementation
// ============================================================================

extern "C" {

JALIUM_API JaliumContext* jalium_context_create(JaliumBackend backend) {
    auto& registry = jalium::GetBackendRegistry();

    // If AUTO, try to find the best available backend
    JaliumBackend actualBackend = backend;
    if (backend == JALIUM_BACKEND_AUTO) {
        // Prefer D3D12 on Windows
        if (jalium_is_backend_available(JALIUM_BACKEND_D3D12)) {
            actualBackend = JALIUM_BACKEND_D3D12;
        } else if (jalium_is_backend_available(JALIUM_BACKEND_D3D11)) {
            actualBackend = JALIUM_BACKEND_D3D11;
        } else if (jalium_is_backend_available(JALIUM_BACKEND_VULKAN)) {
            actualBackend = JALIUM_BACKEND_VULKAN;
        } else if (jalium_is_backend_available(JALIUM_BACKEND_OPENGL)) {
            actualBackend = JALIUM_BACKEND_OPENGL;
        } else {
            // No backend available
            return nullptr;
        }
    }

    // Get the factory for the requested backend
    auto factory = registry.GetFactory(actualBackend);
    if (!factory) {
        return nullptr;
    }

    // Create the backend implementation
    // Note: factory() returns IRenderBackend* (C-style), which is jalium::IRenderBackend* in C++
    auto* rawBackend = reinterpret_cast<jalium::IRenderBackend*>(factory());
    if (!rawBackend) {
        return nullptr;
    }
    auto backendImpl = std::unique_ptr<jalium::IRenderBackend>(rawBackend);

    // Create and return the context
    auto* ctx = new jalium::Context(actualBackend, std::move(backendImpl));
    return reinterpret_cast<JaliumContext*>(ctx);
}

JALIUM_API void jalium_context_destroy(JaliumContext* ctx) {
    if (ctx) {
        delete reinterpret_cast<jalium::Context*>(ctx);
    }
}

JALIUM_API JaliumBackend jalium_context_get_backend(JaliumContext* ctx) {
    if (!ctx) return JALIUM_BACKEND_AUTO;
    return reinterpret_cast<jalium::Context*>(ctx)->GetBackend();
}

JALIUM_API JaliumResult jalium_context_get_last_error(JaliumContext* ctx) {
    if (!ctx) return JALIUM_ERROR_INVALID_ARGUMENT;
    return reinterpret_cast<jalium::Context*>(ctx)->GetLastError();
}

JALIUM_API const wchar_t* jalium_context_get_error_message(JaliumContext* ctx) {
    if (!ctx) return nullptr;
    return reinterpret_cast<jalium::Context*>(ctx)->GetErrorMessage();
}

} // extern "C"
