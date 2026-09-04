#include "ffpe_native.h"
#include <cassert>

int main() {
    assert(ffpe_abi_version() == 1);
    assert(ffpe_get_memory_info(nullptr) == 1);
    assert(ffpe_get_cpu_times(nullptr) == 1);
    assert(ffpe_get_process_priority(0, nullptr) == 1);

    ffpe_memory_info memory{};
    assert(ffpe_get_memory_info(&memory) == 0);
    assert(memory.available_physical_bytes <= memory.total_physical_bytes);

    ffpe_cpu_times cpu{};
    assert(ffpe_get_cpu_times(&cpu) == 0);
    assert(cpu.kernel_100ns + cpu.user_100ns >= cpu.idle_100ns);
    return 0;
}
