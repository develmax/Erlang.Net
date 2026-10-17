#ifndef ERLANG_NET_NIF_BRIDGE_API_H
#define ERLANG_NET_NIF_BRIDGE_API_H

#if !defined(__linux__) || !defined(__x86_64__)
#error This source profile is Linux x64 only; Windows fixed callbacks bind directly.
#endif

#include "erl_nif.h"
#include <stdarg.h>
#include <stdint.h>

_Static_assert(sizeof(long) == 8, "Linux bridge requires LP64 long");
_Static_assert(sizeof(void *) == 8, "Linux bridge requires x64 pointers");

#define NIF_BRIDGE_EXPORT __attribute__((visibility("default")))
#define NIF_BRIDGE_VERSION UINT32_C(2)

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
    int (*is_atom)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_binary)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_ref)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_fun)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_pid)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_port)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_empty_list)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_list)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_tuple)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_number)(ErlNifEnv *, ERL_NIF_TERM);
    int (*is_map)(ErlNifEnv *, ERL_NIF_TERM);
    int (*get_list_cell)(ErlNifEnv *, ERL_NIF_TERM, ERL_NIF_TERM *, ERL_NIF_TERM *);
    ERL_NIF_TERM (*make_tuple_from_array)(ErlNifEnv *, const ERL_NIF_TERM *, unsigned);
    ERL_NIF_TERM (*make_list_from_array)(ErlNifEnv *, const ERL_NIF_TERM *, unsigned);
    int (*get_ulong)(ErlNifEnv *, ERL_NIF_TERM, unsigned long *);
    int (*get_double)(ErlNifEnv *, ERL_NIF_TERM, double *);
    int (*get_uint)(ErlNifEnv *, ERL_NIF_TERM, unsigned int *);
    int (*get_long)(ErlNifEnv *, ERL_NIF_TERM, long *);
    int (*get_int64)(ErlNifEnv *, ERL_NIF_TERM, ErlNifSInt64 *);
    int (*get_uint64)(ErlNifEnv *, ERL_NIF_TERM, ErlNifUInt64 *);
    ERL_NIF_TERM (*make_ulong)(ErlNifEnv *, unsigned long);
    ERL_NIF_TERM (*make_double)(ErlNifEnv *, double);
    ERL_NIF_TERM (*make_uint)(ErlNifEnv *, unsigned int);
    ERL_NIF_TERM (*make_long)(ErlNifEnv *, long);
    ERL_NIF_TERM (*make_int64)(ErlNifEnv *, ErlNifSInt64);
    ERL_NIF_TERM (*make_uint64)(ErlNifEnv *, ErlNifUInt64);
} NifBridgeCallbacks;

_Static_assert(sizeof(NifBridgeCallbacks) == 312, "Fixed bridge table size mismatch");

/* Caller owns an immutable, process-lifetime table. No replacement/unregister. */
NIF_BRIDGE_EXPORT int nif_bridge_register(const NifBridgeCallbacks *callbacks);
NIF_BRIDGE_EXPORT int nif_bridge_read_term(NifBridgeCursor *cursor, ERL_NIF_TERM *term);
const NifBridgeCallbacks *nif_bridge_callbacks(void);

#endif
