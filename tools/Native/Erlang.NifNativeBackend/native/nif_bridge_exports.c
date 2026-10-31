#include "nif_bridge_api.h"
#include <stdatomic.h>
#include <stdlib.h>

static _Atomic(const NifBridgeCallbacks *) published_callbacks;

NIF_BRIDGE_EXPORT int nif_bridge_register(const NifBridgeCallbacks *callbacks)
{
    const NifBridgeCallbacks *expected = NULL;

    if (callbacks == NULL || callbacks->version != NIF_BRIDGE_VERSION ||
        callbacks->size != sizeof(NifBridgeCallbacks) ||
        callbacks->private_data == NULL || callbacks->allocate == NULL ||
        callbacks->free_memory == NULL || callbacks->get_int == NULL ||
        callbacks->bad_argument == NULL || callbacks->make_int == NULL ||
        callbacks->make_string == NULL || callbacks->is_exception == NULL ||
        callbacks->has_pending_exception == NULL || callbacks->raise_exception == NULL ||
        callbacks->make_tuple == NULL || callbacks->make_list == NULL ||
        callbacks->is_atom == NULL ||
        callbacks->is_binary == NULL ||
        callbacks->is_ref == NULL ||
        callbacks->is_fun == NULL ||
        callbacks->is_pid == NULL ||
        callbacks->is_port == NULL ||
        callbacks->is_empty_list == NULL ||
        callbacks->is_list == NULL ||
        callbacks->is_tuple == NULL ||
        callbacks->is_number == NULL ||
        callbacks->is_map == NULL ||
        callbacks->get_list_cell == NULL ||
        callbacks->make_tuple_from_array == NULL ||
        callbacks->make_list_from_array == NULL ||
        callbacks->get_ulong == NULL ||
        callbacks->get_double == NULL ||
        callbacks->get_uint == NULL ||
        callbacks->get_long == NULL ||
        callbacks->get_int64 == NULL ||
        callbacks->get_uint64 == NULL ||
        callbacks->make_ulong == NULL ||
        callbacks->make_double == NULL ||
        callbacks->make_uint == NULL ||
        callbacks->make_long == NULL ||
        callbacks->make_int64 == NULL ||
        callbacks->make_uint64 == NULL ||
        callbacks->get_map_size == NULL ||
        callbacks->make_ref == NULL || callbacks->alloc_env == NULL ||
        callbacks->free_env == NULL || callbacks->clear_env == NULL ||
        callbacks->make_copy == NULL || callbacks->thread_type521 == NULL ||
        callbacks->open_resource_type527 == NULL ||
        callbacks->alloc_resource527 == NULL ||
        callbacks->release_resource527 == NULL ||
        callbacks->make_resource527 == NULL ||
        callbacks->get_resource527 == NULL ||
        callbacks->sizeof_resource527 == NULL ||
        callbacks->keep_resource527 == NULL ||
        callbacks->make_resource_binary527 == NULL ||
        callbacks->open_resource_type_x527 == NULL ||
        callbacks->init_resource_type527 == NULL ||
        callbacks->dynamic_resource_call527 == NULL || callbacks->make_list_cell539 == NULL ||
        callbacks->get_string719 == NULL || callbacks->get_tuple719 == NULL ||
        callbacks->make_atom719 == NULL || callbacks->format_term791 == NULL)
        return 1;

    if (atomic_compare_exchange_strong_explicit(&published_callbacks, &expected, callbacks,
                                               memory_order_release, memory_order_acquire))
        return 0;

    return expected == callbacks ? 0 : 2;
}

const NifBridgeCallbacks *nif_bridge_callbacks(void)
{
    const NifBridgeCallbacks *callbacks = atomic_load_explicit(&published_callbacks, memory_order_acquire);

    /* Configuration failure, not a fabricated Erlang badarg or successful allocation. */
    if (callbacks == NULL)
        abort();

    return callbacks;
}

NIF_BRIDGE_EXPORT void *enif_priv_data(ErlNifEnv *env)
{
    return nif_bridge_callbacks()->private_data(env);
}

NIF_BRIDGE_EXPORT void *enif_alloc(size_t size)
{
    return nif_bridge_callbacks()->allocate(size);
}

NIF_BRIDGE_EXPORT void enif_free(void *pointer)
{
    nif_bridge_callbacks()->free_memory(pointer);
}

NIF_BRIDGE_EXPORT int enif_get_int(ErlNifEnv *env, ERL_NIF_TERM term, int *value)
{
    return nif_bridge_callbacks()->get_int(env, term, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_badarg(ErlNifEnv *env)
{
    return nif_bridge_callbacks()->bad_argument(env);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_int(ErlNifEnv *env, int value)
{
    return nif_bridge_callbacks()->make_int(env, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_string(ErlNifEnv *env, const char *text, ErlNifCharEncoding encoding)
{
    return nif_bridge_callbacks()->make_string(env, text, encoding);
}

NIF_BRIDGE_EXPORT int enif_is_exception(ErlNifEnv *env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_exception(env, term);
}

NIF_BRIDGE_EXPORT int enif_has_pending_exception(ErlNifEnv *env, ERL_NIF_TERM *reason)
{
    return nif_bridge_callbacks()->has_pending_exception(env, reason);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_raise_exception(ErlNifEnv *env, ERL_NIF_TERM reason)
{
    return nif_bridge_callbacks()->raise_exception(env, reason);
}

/* Export both public names as well as the LP64 aliases used by pinned headers. */
#undef enif_get_int64
#undef enif_get_uint64
#undef enif_make_int64
#undef enif_make_uint64

NIF_BRIDGE_EXPORT int enif_is_atom(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_atom(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_binary(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_binary(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_ref(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_ref(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_fun(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_fun(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_pid(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_pid(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_port(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_port(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_empty_list(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_empty_list(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_list(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_list(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_tuple(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_tuple(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_number(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_number(env, term);
}

NIF_BRIDGE_EXPORT int enif_is_map(ErlNifEnv * env, ERL_NIF_TERM term)
{
    return nif_bridge_callbacks()->is_map(env, term);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_list_cell(ErlNifEnv *env, ERL_NIF_TERM head, ERL_NIF_TERM tail)
{
    return nif_bridge_callbacks()->make_list_cell539(env, head, tail);
}

NIF_BRIDGE_EXPORT int enif_get_list_cell(ErlNifEnv * env, ERL_NIF_TERM term, ERL_NIF_TERM * head, ERL_NIF_TERM * tail)
{
    return nif_bridge_callbacks()->get_list_cell(env, term, head, tail);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_tuple_from_array(ErlNifEnv * env, const ERL_NIF_TERM * terms, unsigned count)
{
    return nif_bridge_callbacks()->make_tuple_from_array(env, terms, count);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_list_from_array(ErlNifEnv * env, const ERL_NIF_TERM * terms, unsigned count)
{
    return nif_bridge_callbacks()->make_list_from_array(env, terms, count);
}

NIF_BRIDGE_EXPORT int enif_get_ulong(ErlNifEnv * env, ERL_NIF_TERM term, unsigned long * value)
{
    return nif_bridge_callbacks()->get_ulong(env, term, value);
}

NIF_BRIDGE_EXPORT int enif_get_double(ErlNifEnv * env, ERL_NIF_TERM term, double * value)
{
    return nif_bridge_callbacks()->get_double(env, term, value);
}

NIF_BRIDGE_EXPORT int enif_get_uint(ErlNifEnv * env, ERL_NIF_TERM term, unsigned int * value)
{
    return nif_bridge_callbacks()->get_uint(env, term, value);
}

NIF_BRIDGE_EXPORT int enif_get_long(ErlNifEnv * env, ERL_NIF_TERM term, long * value)
{
    return nif_bridge_callbacks()->get_long(env, term, value);
}

NIF_BRIDGE_EXPORT int enif_get_int64(ErlNifEnv * env, ERL_NIF_TERM term, ErlNifSInt64 * value)
{
    return nif_bridge_callbacks()->get_int64(env, term, value);
}

NIF_BRIDGE_EXPORT int enif_get_uint64(ErlNifEnv * env, ERL_NIF_TERM term, ErlNifUInt64 * value)
{
    return nif_bridge_callbacks()->get_uint64(env, term, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_ulong(ErlNifEnv * env, unsigned long value)
{
    return nif_bridge_callbacks()->make_ulong(env, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_double(ErlNifEnv * env, double value)
{
    return nif_bridge_callbacks()->make_double(env, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_uint(ErlNifEnv * env, unsigned int value)
{
    return nif_bridge_callbacks()->make_uint(env, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_long(ErlNifEnv * env, long value)
{
    return nif_bridge_callbacks()->make_long(env, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_int64(ErlNifEnv * env, ErlNifSInt64 value)
{
    return nif_bridge_callbacks()->make_int64(env, value);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_uint64(ErlNifEnv * env, ErlNifUInt64 value)
{
    return nif_bridge_callbacks()->make_uint64(env, value);
}

NIF_BRIDGE_EXPORT int enif_get_map_size(ErlNifEnv *env, ERL_NIF_TERM term, size_t *size)
{
    return nif_bridge_callbacks()->get_map_size(env, term, size);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_ref(ErlNifEnv *env)
{
    return nif_bridge_callbacks()->make_ref(env);
}

NIF_BRIDGE_EXPORT ErlNifEnv *enif_alloc_env(void)
{
    return nif_bridge_callbacks()->alloc_env();
}

NIF_BRIDGE_EXPORT void enif_free_env(ErlNifEnv *env)
{
    nif_bridge_callbacks()->free_env(env);
}

NIF_BRIDGE_EXPORT void enif_clear_env(ErlNifEnv *env)
{
    nif_bridge_callbacks()->clear_env(env);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_copy(ErlNifEnv *env, ERL_NIF_TERM source)
{
    return nif_bridge_callbacks()->make_copy(env, source);
}

NIF_BRIDGE_EXPORT int enif_thread_type(void)
{
    return nif_bridge_callbacks()->thread_type521();
}

NIF_BRIDGE_EXPORT ErlNifResourceType *enif_open_resource_type(ErlNifEnv *env, const char *module, const char *name, ErlNifResourceDtor *dtor, ErlNifResourceFlags flags, ErlNifResourceFlags *tried)
{
    return nif_bridge_callbacks()->open_resource_type527(env, module, name, dtor, flags, tried);
}

NIF_BRIDGE_EXPORT void *enif_alloc_resource(ErlNifResourceType *type, size_t size)
{
    return nif_bridge_callbacks()->alloc_resource527(type, size);
}

NIF_BRIDGE_EXPORT void enif_release_resource(void *resource)
{
    nif_bridge_callbacks()->release_resource527(resource);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_resource(ErlNifEnv *env, void *resource)
{
    return nif_bridge_callbacks()->make_resource527(env, resource);
}

NIF_BRIDGE_EXPORT int enif_get_resource(ErlNifEnv *env, ERL_NIF_TERM term, ErlNifResourceType *type, void **output)
{
    return nif_bridge_callbacks()->get_resource527(env, term, type, output);
}

NIF_BRIDGE_EXPORT size_t enif_sizeof_resource(void *resource)
{
    return nif_bridge_callbacks()->sizeof_resource527(resource);
}

NIF_BRIDGE_EXPORT void enif_keep_resource(void *resource)
{
    nif_bridge_callbacks()->keep_resource527(resource);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_resource_binary(ErlNifEnv *env, void *resource, const void *data, size_t size)
{
    return nif_bridge_callbacks()->make_resource_binary527(env, resource, data, size);
}

NIF_BRIDGE_EXPORT ErlNifResourceType *enif_open_resource_type_x(ErlNifEnv *env, const char *name, const ErlNifResourceTypeInit *init, ErlNifResourceFlags flags, ErlNifResourceFlags *tried)
{
    return nif_bridge_callbacks()->open_resource_type_x527(env, name, init, flags, tried);
}

NIF_BRIDGE_EXPORT ErlNifResourceType *enif_init_resource_type(ErlNifEnv *env, const char *name, const ErlNifResourceTypeInit *init, ErlNifResourceFlags flags, ErlNifResourceFlags *tried)
{
    return nif_bridge_callbacks()->init_resource_type527(env, name, init, flags, tried);
}

NIF_BRIDGE_EXPORT int enif_dynamic_resource_call(ErlNifEnv *env, ERL_NIF_TERM module, ERL_NIF_TERM name, ERL_NIF_TERM resource, void *data)
{
    return nif_bridge_callbacks()->dynamic_resource_call527(env, module, name, resource, data);
}

NIF_BRIDGE_EXPORT int enif_get_string(ErlNifEnv *env, ERL_NIF_TERM term, char *buffer, unsigned size, ErlNifCharEncoding encoding)
{
    return nif_bridge_callbacks()->get_string719(env, term, buffer, size, encoding);
}

NIF_BRIDGE_EXPORT int enif_get_tuple(ErlNifEnv *env, ERL_NIF_TERM term, int *arity, const ERL_NIF_TERM **array)
{
    return nif_bridge_callbacks()->get_tuple719(env, term, arity, array);
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_atom(ErlNifEnv *env, const char *name)
{
    return nif_bridge_callbacks()->make_atom719(env, name);
}

/* The managed table is bound to THIS namespace/VM. No native thread-local
 * variable or arbitrary first NIF image supplies allocation authority. */
static _Atomic(const NifBridgeMutex795Callbacks *) published_mutex795_callbacks;
NIF_BRIDGE_EXPORT int nif_bridge_mutex795_register(const NifBridgeMutex795Callbacks *callbacks)
{
    const NifBridgeMutex795Callbacks *expected = NULL;
    if (callbacks == NULL || callbacks->version != NIF_BRIDGE_MUTEX795_VERSION
        || callbacks->size != sizeof(NifBridgeMutex795Callbacks)
        || callbacks->create == NULL || callbacks->destroy == NULL
        || callbacks->trylock == NULL || callbacks->lock == NULL
        || callbacks->unlock == NULL || callbacks->name == NULL)
        return 1;
    if (atomic_compare_exchange_strong_explicit(&published_mutex795_callbacks, &expected, callbacks,
            memory_order_release, memory_order_acquire)) return 0;
    return expected == callbacks ? 0 : 2;
}
static const NifBridgeMutex795Callbacks *mutex795_callbacks(void)
{
    const NifBridgeMutex795Callbacks *callbacks = atomic_load_explicit(&published_mutex795_callbacks, memory_order_acquire);
    if (callbacks == NULL) abort(); /* Genuine missing host configuration. */
    return callbacks;
}
NIF_BRIDGE_EXPORT ErlNifMutex *enif_mutex_create(char *name)
{
    return mutex795_callbacks()->create(name);
}
NIF_BRIDGE_EXPORT void enif_mutex_destroy(ErlNifMutex *mutex)
{
    mutex795_callbacks()->destroy(mutex);
}
NIF_BRIDGE_EXPORT int enif_mutex_trylock(ErlNifMutex *mutex)
{
    return mutex795_callbacks()->trylock(mutex);
}
NIF_BRIDGE_EXPORT void enif_mutex_lock(ErlNifMutex *mutex)
{
    mutex795_callbacks()->lock(mutex);
}
NIF_BRIDGE_EXPORT void enif_mutex_unlock(ErlNifMutex *mutex)
{
    mutex795_callbacks()->unlock(mutex);
}
NIF_BRIDGE_EXPORT char *enif_mutex_name(ErlNifMutex *mutex)
{
    return mutex795_callbacks()->name(mutex);
}
