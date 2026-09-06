#pragma once
#include <cstdint>

#if defined(_WIN32)
  #define FFPE_API extern "C" __declspec(dllexport)
#else
  #define FFPE_API extern "C"
#endif

struct ffpe_memory_info {
    std::uint64_t total_physical_bytes;
    std::uint64_t available_physical_bytes;
};

struct ffpe_cpu_times {
    std::uint64_t idle_100ns;
    std::uint64_t kernel_100ns;
    std::uint64_t user_100ns;
};

FFPE_API int ffpe_abi_version();
FFPE_API int ffpe_get_memory_info(ffpe_memory_info* out_info);
FFPE_API int ffpe_get_cpu_times(ffpe_cpu_times* out_times);
FFPE_API int ffpe_get_process_priority(std::uint32_t process_id, int* out_priority_class);
FFPE_API int ffpe_set_process_priority(std::uint32_t process_id, int priority_class);
