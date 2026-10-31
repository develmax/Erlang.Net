#define MUTEX_VERSION795 43
#define MUTEX_FAIL795 0
#include "erl_nif.h"
#include <string.h>
#ifndef MUTEX_VERSION795
#define MUTEX_VERSION795 42
#endif
#ifndef MUTEX_FAIL795
#define MUTEX_FAIL795 0
#endif

typedef struct { ErlNifMutex *mutex; int calls; } State795;
static const char name795[] = "upgrade-mutex795";

static int load795(ErlNifEnv *env, void **data, ERL_NIF_TERM info)
{
    ErlNifMutex *mutex;
    (void)env; (void)info;
    mutex = enif_mutex_create((char *)name795);
    if (!mutex) return 11;
#if MUTEX_FAIL795
        /* Deliberate failed-init mutex containment. No unload callback is
         * promised and NO unrelated enif_alloc userdata is leaked here. */
        *data = mutex;
        return 1;
#else
    State795 *state = enif_alloc(sizeof(*state));
    if (!state) { enif_mutex_destroy(mutex); return 12; }
    state->mutex = mutex;
    state->calls = 0;
    *data = state;
    return 0;
#endif
}

static int upgrade795(ErlNifEnv *env, void **fresh, void **old, ERL_NIF_TERM info)
{
    State795 *state = (State795 *)*old;
    (void)env; (void)info;
    if (!state || !state->mutex) return 21;
    /* Transfer the REAL original allocation graph, not copied bytes or a
     * second mutex. Old unload receives NULL; new image is sole disposer. */
    *fresh = state;
    *old = NULL;
    return 0;
}

static void unload795(ErlNifEnv *env, void *data)
{
    State795 *state = data;
    (void)env;
    if (!state) return;
    if (state->mutex) enif_mutex_destroy(state->mutex);
    enif_free(state);
}

static ERL_NIF_TERM probe795(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    State795 *state = enif_priv_data(env);
    int result, named;
    (void)argc; (void)argv;
    if (!state || !state->mutex) return enif_make_atom(env, "destroyed");
    result = enif_mutex_trylock(state->mutex);
    if (result != 0) return enif_make_int(env, result);
    named = strcmp(enif_mutex_name(state->mutex), name795) == 0;
    state->calls++;
    enif_mutex_unlock(state->mutex);
    return enif_make_tuple3(env, enif_make_int(env, MUTEX_VERSION795),
        enif_make_int(env, state->calls), enif_make_int(env, named));
}

static ERL_NIF_TERM destroy795(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    State795 *state = enif_priv_data(env);
    (void)argc; (void)argv;
    if (!state || !state->mutex) return enif_make_badarg(env);
    enif_mutex_destroy(state->mutex);
    state->mutex = NULL;
    return enif_make_atom(env, "ok");
}

static ErlNifFunc functions795[] = {
    {"probe", 0, probe795, 0}, {"destroy", 0, destroy795, 0}
};
ERL_NIF_INIT(mutex_lifecycle795, functions795, load795, NULL, upgrade795, unload795)
