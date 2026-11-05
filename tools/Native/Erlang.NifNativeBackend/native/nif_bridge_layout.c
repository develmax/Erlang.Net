#include "nif_bridge_api.h"
#include <stddef.h>

/* Schema 1: public OTP ABI plus our fixed bridge table. No ERTS internals. */
NIF_BRIDGE_EXPORT int nif_bridge_platform_layout(unsigned schema, size_t *values, unsigned count)
{
    const size_t layout[] = {
        sizeof(void *), sizeof(long), sizeof(ErlNifEntry), sizeof(ErlNifFunc),
        sizeof(ErlNifBinary), sizeof(NifBridgeCallbacks),
        offsetof(ErlNifEntry, name), offsetof(ErlNifEntry, funcs),
        offsetof(ErlNifEntry, load), offsetof(ErlNifEntry, vm_variant),
        offsetof(ErlNifEntry, sizeof_ErlNifResourceTypeInit),
        offsetof(ErlNifEntry, min_erts), sizeof(ErlNifEvent)
    };
    unsigned index;
    if (schema != 1 || values == NULL || count != sizeof(layout) / sizeof(layout[0]))
        return -1;
    for (index = 0; index < count; index++)
        values[index] = layout[index];
    return 0;
}
