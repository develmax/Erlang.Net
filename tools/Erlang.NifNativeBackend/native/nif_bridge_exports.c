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
        callbacks->make_tuple == NULL || callbacks->make_list == NULL)
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
