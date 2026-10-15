#ifndef ERLANG_NET_REALLOCATION_BRIDGE_H
#define ERLANG_NET_REALLOCATION_BRIDGE_H

#if !defined(__linux__) || !defined(__x86_64__)
#error This additive reallocation extension profile supports Linux x64 only.
#endif

#include "erl_nif.h"
#include <stdint.h>

#define REALLOCATION_BRIDGE_EXPORT __attribute__((visibility("default")))
#define REALLOCATION_BRIDGE_VERSION UINT32_C(1)

typedef struct NifReallocationBridgeCallbacks {
    uint32_t version;
    uint32_t size;
    void *(*reallocate)(void *, size_t);
} NifReallocationBridgeCallbacks;

/* Immutable process-lifetime extension, independent of the old v1 bridge table. */
REALLOCATION_BRIDGE_EXPORT int erlang_net_reallocation_register(const NifReallocationBridgeCallbacks *callbacks);

#endif
