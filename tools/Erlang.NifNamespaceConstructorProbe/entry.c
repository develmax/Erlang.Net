#include <stdint.h>
extern uintptr_t probe_allocate(void);
static uintptr_t constructor_domain;
__attribute__((constructor)) static void initialize(void)
{
    constructor_domain=probe_allocate();
}
uintptr_t probe_read_constructor_domain(void) { return constructor_domain; }
uintptr_t probe_read_live_domain(void) { return probe_allocate(); }
