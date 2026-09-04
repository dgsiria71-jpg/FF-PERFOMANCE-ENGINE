#include "ffpe_native.h"

#if defined(_WIN32)
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif

namespace {
constexpr int kAbiVersion = 1;
#if defined(_WIN32)
std::uint64_t as_u64(const FILETIME& value) {
    ULARGE_INTEGER converted{};
    converted.LowPart = value.dwLowDateTime;
    converted.HighPart = value.dwHighDateTime;
    return converted.QuadPart;
}
#endif
}

int ffpe_abi_version() { return kAbiVersion; }

int ffpe_get_memory_info(ffpe_memory_info* out_info) {
    if (out_info == nullptr) return 1;
#if defined(_WIN32)
    MEMORYSTATUSEX status{};
    status.dwLength = sizeof(status);
    if (!GlobalMemoryStatusEx(&status)) return 2;
    out_info->total_physical_bytes = status.ullTotalPhys;
    out_info->available_physical_bytes = status.ullAvailPhys;
    return 0;
#else
    out_info->total_physical_bytes = 0;
    out_info->available_physical_bytes = 0;
    return 0;
#endif
}

int ffpe_get_cpu_times(ffpe_cpu_times* out_times) {
    if (out_times == nullptr) return 1;
#if defined(_WIN32)
    FILETIME idle{}, kernel{}, user{};
    if (!GetSystemTimes(&idle, &kernel, &user)) return 2;
    out_times->idle_100ns = as_u64(idle);
    out_times->kernel_100ns = as_u64(kernel);
    out_times->user_100ns = as_u64(user);
    return 0;
#else
    out_times->idle_100ns = 0;
    out_times->kernel_100ns = 1;
    out_times->user_100ns = 1;
    return 0;
#endif
}

int ffpe_set_process_priority(std::uint32_t process_id, int priority_class) {
#if defined(_WIN32)
    const DWORD allowed[] = {IDLE_PRIORITY_CLASS, BELOW_NORMAL_PRIORITY_CLASS, NORMAL_PRIORITY_CLASS,
                             ABOVE_NORMAL_PRIORITY_CLASS, HIGH_PRIORITY_CLASS};
    bool valid = false;
    for (const DWORD item : allowed) if (static_cast<int>(item) == priority_class) valid = true;
    if (!valid) return 3;
    HANDLE process = OpenProcess(PROCESS_SET_INFORMATION, FALSE, process_id);
    if (process == nullptr) return 2;
    const BOOL ok = SetPriorityClass(process, static_cast<DWORD>(priority_class));
    CloseHandle(process);
    return ok ? 0 : 2;
#else
    (void)process_id;
    (void)priority_class;
    return 4;
#endif
}
