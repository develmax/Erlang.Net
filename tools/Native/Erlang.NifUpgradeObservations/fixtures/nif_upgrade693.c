#include "erl_nif.h"
#include <stdio.h>
#include <string.h>

/* Unbuilt original/managed fixture candidate. Build as physically distinct A/B
 * images with CASE_VERSION=42/43; optional NO_UPGRADE and WITH_RESOURCE. */
#ifndef CASE_VERSION
#define CASE_VERSION 42
#endif
#ifndef WITH_RESOURCE
#define WITH_RESOURCE 0
#endif

enum { KeepOld = 0, RelinquishOld = 1, VetoPositive = 2, VetoNegative = 3 };
enum { LoadEvent = 1, UpgradeEnter = 2, UpgradeAccept = 3,
       UpgradeVeto = 4, UnloadEvent = 5, ResourceDtor = 6, ForbiddenReload = 7 };
typedef struct { int version; int counter; } State693;
static char log_path693[4096];
#if WITH_RESOURCE
static ErlNifResourceType* resource_type693;
#endif

/* No pointer-address normalization: log semantic states as fixed ASCII numbers. */
static int event693(int event, int a, int b)
{
    FILE* file = fopen(log_path693, "ab");
    if (!file) return 0;
    int ok = fprintf(file, "%d %d %d %d\n", event, CASE_VERSION, a, b) > 0;
    return fclose(file) == 0 && ok;
}

static int info693(ErlNifEnv* env, ERL_NIF_TERM info, int* mode)
{
    int arity;
    const ERL_NIF_TERM* tuple;
    return enif_get_tuple(env, info, &arity, &tuple) && arity == 2
        && enif_get_string(env, tuple[0], log_path693,
            (unsigned)sizeof(log_path693), ERL_NIF_LATIN1) > 0
        && enif_get_int(env, tuple[1], mode)
        && *mode >= KeepOld && *mode <= VetoNegative;
}

#if WITH_RESOURCE
static void resource_dtor693(ErlNifEnv* env, void* payload)
{
    (void)env;
    (void)event693(ResourceDtor, *(int*)payload, 0);
}

#endif

static int open_resource693(ErlNifEnv* env, ErlNifResourceFlags flags)
{
#if WITH_RESOURCE
    ErlNifResourceFlags tried;
    resource_type693 = enif_open_resource_type(env, NULL, "upgrade_resource693",
        resource_dtor693, flags, &tried);
    return resource_type693 != NULL && tried == flags;
#else
    (void)env; (void)flags;
    return 1;
#endif
}

static int load693(ErlNifEnv* env, void** data, ERL_NIF_TERM info)
{
    int mode;
    if (!info693(env, info, &mode)) return 41;
    State693* state = enif_alloc(sizeof(*state));
    if (!state) return 42;
    state->version = CASE_VERSION;
    state->counter = 17;
    if (!open_resource693(env, ERL_NIF_RT_CREATE)
        || !event693(LoadEvent, state->counter, mode)) {
        enif_free(state);
        return 43;
    }
    *data = state;
    return 0;
}

#ifndef NO_UPGRADE
static int upgrade693(ErlNifEnv* env, void** new_data, void** old_data,
    ERL_NIF_TERM info)
{
    int mode;
    if (!info693(env, info, &mode)) return 51;
    State693* old = *old_data;
    if (!old || !event693(UpgradeEnter, old->version, old->counter)) return 52;
    State693* fresh = enif_alloc(sizeof(*fresh));
    if (!fresh) return 53;
    fresh->version = CASE_VERSION;
    fresh->counter = old->counter + 1;
    *new_data = fresh;
    if (!open_resource693(env, ERL_NIF_RT_TAKEOVER)) {
        enif_free(fresh);
        *new_data = NULL;
        return 54;
    }
    if (mode == VetoPositive || mode == VetoNegative) {
        /* Native code intentionally leaves the old CELL null. OTP must restore
         * its original value on veto. It cannot repair freed/mutated contents. */
        *old_data = NULL;
        enif_free(fresh);
        *new_data = NULL;
        (void)event693(UpgradeVeto, mode, 0);
        return mode == VetoPositive ? 7 : -13;
    }
    if (mode == RelinquishOld) {
        *old_data = NULL;
        enif_free(old);
    }
    (void)event693(UpgradeAccept, mode, fresh->counter);
    return 0;
}

#endif

static void unload693(ErlNifEnv* env, void* data)
{
    (void)env;
    State693* state = data;
    (void)event693(UnloadEvent, state ? state->version : 0,
        state ? state->counter : 0);
    if (state) enif_free(state);
}

static int reload693(ErlNifEnv* env, void** data, ERL_NIF_TERM info)
{
    (void)env; (void)data; (void)info;
    (void)event693(ForbiddenReload, 0, 0);
    return 99; /* OTP29 must reject reload before entering this callback. */
}

static ERL_NIF_TERM identity693(ErlNifEnv* env, int argc,
    const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_int(env, CASE_VERSION);
}

static ERL_NIF_TERM state693(ErlNifEnv* env, int argc,
    const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    State693* state = enif_priv_data(env);
    return enif_make_tuple3(env, enif_make_int(env, CASE_VERSION),
        enif_make_int(env, state ? state->version : 0),
        enif_make_int(env, state ? state->counter : 0));
}

static ERL_NIF_TERM resource693(ErlNifEnv* env, int argc,
    const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
#if WITH_RESOURCE
    int* payload = enif_alloc_resource(resource_type693, sizeof(*payload));
    if (!payload) return enif_make_badarg(env);
    *payload = 123;
    ERL_NIF_TERM result = enif_make_resource(env, payload);
    enif_release_resource(payload);
    return result;
#else
    return enif_make_atom(env, "resource_disabled");
#endif
}

static ErlNifFunc functions693[] = {
    {"identity", 0, identity693, 0},
    {"state", 0, state693, 0},
    {"resource", 0, resource693, 0}
};
#ifdef NO_UPGRADE
ERL_NIF_INIT(nif_upgrade693, functions693, load693, reload693, NULL, unload693)
#else
ERL_NIF_INIT(nif_upgrade693, functions693, load693, reload693, upgrade693, unload693)
#endif
