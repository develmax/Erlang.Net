#include "nif_reallocation_bridge.h"
#include <stdatomic.h>
#include <stdlib.h>

static _Atomic(const NifReallocationBridgeCallbacks *) published_reallocation_callbacks;

REALLOCATION_BRIDGE_EXPORT int erlang_net_reallocation_register(const NifReallocationBridgeCallbacks *callbacks)
{
    const NifReallocationBridgeCallbacks *expected = NULL;
    if (callbacks == NULL || callbacks->version != REALLOCATION_BRIDGE_VERSION ||
        callbacks->size != sizeof(NifReallocationBridgeCallbacks) || callbacks->reallocate == NULL)
        return 1;
    if (atomic_compare_exchange_strong_explicit(&published_reallocation_callbacks, &expected, callbacks,
                                               memory_order_release, memory_order_acquire))
        return 0;
    return expected == callbacks ? 0 : 2;
}

REALLOCATION_BRIDGE_EXPORT void *enif_realloc(void *pointer, size_t size)
{
    const NifReallocationBridgeCallbacks *callbacks = atomic_load_explicit(&published_reallocation_callbacks, memory_order_acquire);
    if (callbacks == NULL)
        abort();
    return callbacks->reallocate(pointer, size);
}
