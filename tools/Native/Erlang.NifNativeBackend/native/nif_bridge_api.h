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
#define NIF_BRIDGE_VERSION UINT32_C(9)

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
    int (*get_map_size)(ErlNifEnv *, ERL_NIF_TERM, size_t *);
    ERL_NIF_TERM (*make_ref)(ErlNifEnv *);
    ErlNifEnv *(*alloc_env)(void);
    void (*free_env)(ErlNifEnv *);
    void (*clear_env)(ErlNifEnv *);
    ERL_NIF_TERM (*make_copy)(ErlNifEnv *, ERL_NIF_TERM);
    int (*thread_type521)(void);
    ErlNifResourceType *(*open_resource_type527)(ErlNifEnv *env, const char *module, const char *name, ErlNifResourceDtor *dtor, ErlNifResourceFlags flags, ErlNifResourceFlags *tried);
    void *(*alloc_resource527)(ErlNifResourceType *type, size_t size);
    void(*release_resource527)(void *resource);
    ERL_NIF_TERM(*make_resource527)(ErlNifEnv *env, void *resource);
    int(*get_resource527)(ErlNifEnv *env, ERL_NIF_TERM term, ErlNifResourceType *type, void **output);
    size_t(*sizeof_resource527)(void *resource);
    void(*keep_resource527)(void *resource);
    ERL_NIF_TERM(*make_resource_binary527)(ErlNifEnv *env, void *resource, const void *data, size_t size);
    ErlNifResourceType *(*open_resource_type_x527)(ErlNifEnv *env, const char *name, const ErlNifResourceTypeInit *init, ErlNifResourceFlags flags, ErlNifResourceFlags *tried);
    ErlNifResourceType *(*init_resource_type527)(ErlNifEnv *env, const char *name, const ErlNifResourceTypeInit *init, ErlNifResourceFlags flags, ErlNifResourceFlags *tried);
    int(*dynamic_resource_call527)(ErlNifEnv *env, ERL_NIF_TERM module, ERL_NIF_TERM name, ERL_NIF_TERM resource, void *data);
    ERL_NIF_TERM (*make_list_cell539)(ErlNifEnv *, ERL_NIF_TERM, ERL_NIF_TERM);
    int (*get_string719)(ErlNifEnv *, ERL_NIF_TERM, char *, unsigned, ErlNifCharEncoding);
    int (*get_tuple719)(ErlNifEnv *, ERL_NIF_TERM, int *, const ERL_NIF_TERM **);
    ERL_NIF_TERM (*make_atom719)(ErlNifEnv *, const char *);
    int (*format_term791)(uintptr_t, int64_t, int (*)(void *, const char *, size_t), void *);
} NifBridgeCallbacks;

_Static_assert(sizeof(NifBridgeCallbacks) == 496, "Fixed bridge table size mismatch");

/* Optional independent extension. Main ABI9/496 and all old offsets stay
 * unchanged; immutable callbacks live until this actual ELF bridge closes. */
#define NIF_BRIDGE_MUTEX795_VERSION UINT32_C(1)
typedef struct NifBridgeMutex795Callbacks {
    uint32_t version;
    uint32_t size;
    ErlNifMutex *(*create)(char *);
    void (*destroy)(ErlNifMutex *);
    int (*trylock)(ErlNifMutex *);
    void (*lock)(ErlNifMutex *);
    void (*unlock)(ErlNifMutex *);
    char *(*name)(ErlNifMutex *);
} NifBridgeMutex795Callbacks;
_Static_assert(sizeof(NifBridgeMutex795Callbacks) == 56, "Mutex extension fixed layout mismatch");
NIF_BRIDGE_EXPORT int nif_bridge_mutex795_register(const NifBridgeMutex795Callbacks *callbacks);

/* Caller owns an immutable, process-lifetime table. No replacement/unregister. */
NIF_BRIDGE_EXPORT int nif_bridge_register(const NifBridgeCallbacks *callbacks);
NIF_BRIDGE_EXPORT int nif_bridge_read_term(NifBridgeCursor *cursor, ERL_NIF_TERM *term);
const NifBridgeCallbacks *nif_bridge_callbacks(void);

#endif
