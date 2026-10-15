#ifndef ERLANG_NET_NIF_BRIDGE_API_H
#define ERLANG_NET_NIF_BRIDGE_API_H

#if !defined(__linux__) || !defined(__x86_64__)
#error This source profile is Linux x64 only; Windows fixed callbacks bind directly.
#endif

#include "erl_nif.h"
#include <stdarg.h>
#include <stdint.h>

#define NIF_BRIDGE_EXPORT __attribute__((visibility("default")))
#define NIF_BRIDGE_VERSION UINT32_C(1)

typedef struct NifBridgeCursor {
    va_list arguments;
    unsigned remaining;
} NifBridgeCursor;

typedef struct NifBridgeCallbacks {
    uint32_t version;
    uint32_t size;
    void *(*private_data)(ErlNifEnv *);
    void *(*allocate)(size_t);
    void (*free_memory)(void *);
    int (*get_int)(ErlNifEnv *, ERL_NIF_TERM, int *);
    ERL_NIF_TERM (*bad_argument)(ErlNifEnv *);
    ERL_NIF_TERM (*make_int)(ErlNifEnv *, int);
    ERL_NIF_TERM (*make_string)(ErlNifEnv *, const char *, ErlNifCharEncoding);
    int (*is_exception)(ErlNifEnv *, ERL_NIF_TERM);
    int (*has_pending_exception)(ErlNifEnv *, ERL_NIF_TERM *);
    ERL_NIF_TERM (*raise_exception)(ErlNifEnv *, ERL_NIF_TERM);
    ERL_NIF_TERM (*make_tuple)(ErlNifEnv *, unsigned, NifBridgeCursor *);
    ERL_NIF_TERM (*make_list)(ErlNifEnv *, unsigned, NifBridgeCursor *);
} NifBridgeCallbacks;

/* Caller owns an immutable, process-lifetime table. No replacement/unregister. */
NIF_BRIDGE_EXPORT int nif_bridge_register(const NifBridgeCallbacks *callbacks);
NIF_BRIDGE_EXPORT int nif_bridge_read_term(NifBridgeCursor *cursor, ERL_NIF_TERM *term);
const NifBridgeCallbacks *nif_bridge_callbacks(void);

#endif
