#include "erl_nif.h"
#define PROBE_EXPORT __attribute__((visibility("default")))

/* Test infrastructure only: exercises SysV register/stack argument traversal. */
PROBE_EXPORT ERL_NIF_TERM nif_bridge_probe_tuple(ErlNifEnv *env, const ERL_NIF_TERM *v)
{
    return enif_make_tuple(env, 17, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7],
                           v[8], v[9], v[10], v[11], v[12], v[13], v[14], v[15], v[16]);
}

PROBE_EXPORT ERL_NIF_TERM nif_bridge_probe_list(ErlNifEnv *env, const ERL_NIF_TERM *v)
{
    return enif_make_list(env, 17, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7],
                          v[8], v[9], v[10], v[11], v[12], v[13], v[14], v[15], v[16]);
}

PROBE_EXPORT ERL_NIF_TERM nif_bridge_probe_empty_tuple(ErlNifEnv *env)
{
    return enif_make_tuple(env, 0);
}

PROBE_EXPORT ERL_NIF_TERM nif_bridge_probe_empty_list(ErlNifEnv *env)
{
    return enif_make_list(env, 0);
}

PROBE_EXPORT ERL_NIF_TERM nif_bridge_probe_pending(ErlNifEnv *env, const ERL_NIF_TERM *v)
{
    ERL_NIF_TERM reason;

    if (enif_has_pending_exception(env, NULL))
        return enif_make_badarg(env);
    (void)enif_raise_exception(env, v[0]);
    (void)enif_raise_exception(env, v[1]);
    if (!enif_has_pending_exception(env, &reason) || reason != v[1] ||
        !enif_is_exception(env, (ERL_NIF_TERM)0) || enif_is_exception(env, v[2]))
        return enif_make_badarg(env);

    /* Deliberately ignore the pending exception and return an ordinary term. */
    return v[2];
}
