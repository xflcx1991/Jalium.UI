// Windows stubs for the Android notification API.
//
// jalium.native.platform's Android implementation (platform_android.cpp)
// exports six jalium_notification_* C symbols that bridge to JNI / Java's
// NotificationManager. The matching managed bindings live in
// Notifications.Android.cs and are referenced — even if not invoked — from
// SystemNotificationManager.CreateBackend's `if (PlatformFactory.IsAndroid)`
// branch.
//
// Under NativeAOT static linking on Windows the trimmer keeps every
// LibraryImport site that ILC can't prove unreachable, so link.exe insists
// on resolving these symbols inside jalium.native.platform.static.lib.
// Providing no-op Windows stubs satisfies the linker; the Windows runtime
// always picks WindowsNotificationBackend (WinRT COM) so these stubs are
// never actually invoked.

#ifdef _WIN32

#include <stdint.h>

extern "C" {

int jalium_notification_init(const char* /*appId*/, const char* /*appName*/, const char* /*channelId*/) {
    return -1;
}

int jalium_notification_show(
    int /*id*/,
    const char* /*title*/,
    const char* /*body*/,
    const char* /*iconPath*/,
    const char* /*imagePath*/,
    const char* /*channelId*/,
    int /*priority*/,
    int /*silent*/,
    const char* /*tag*/,
    const char* /*group*/) {
    return -1;
}

void jalium_notification_add_action(int /*id*/, const char* /*actionId*/, const char* /*label*/) {
}

void jalium_notification_cancel(int /*id*/, const char* /*tag*/) {
}

void jalium_notification_cancel_all(void) {
}

int jalium_notification_is_available(void) {
    return 0;
}

} // extern "C"

#endif // _WIN32
