#pragma once

#include "jalium_api.h"

#ifdef __cplusplus
extern "C" {
#endif

/// Static-only entry point that registers every backend compiled into this
/// AOT image with the core BackendRegistry. Must be called once from managed
/// code before the first jalium_context_create().
///
/// In dynamic-DLL builds backend registration is performed automatically by
/// each backend's DllMain, so this function is irrelevant there. It is only
/// emitted into the .lib produced by jalium.native.aot.static.vcxproj.
JALIUM_API void jalium_aot_register_all_backends(void);

#ifdef __cplusplus
}
#endif
