#include <stdint.h>
extern uintptr_t probe_domain_ref(void);
uintptr_t probe_read_domain(void) { return probe_domain_ref(); }
