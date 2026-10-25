#include "erl_nif.h"
#include <limits.h>

typedef struct fixture_state {
    int successful_calls;
} fixture_state;

static int unload_count;

static int load_fixture(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    int marker;
    fixture_state *state;

    if (!enif_get_int(env, info, &marker) || marker != 42)
        return 1;

    state = enif_alloc(sizeof(*state));
    if (!state)
        return 1;

    state->successful_calls = 0;
    *private_data = state;
    return 0;
}

static void unload_fixture(ErlNifEnv *env, void *private_data)
{
    (void)env;
    enif_free(private_data);
    unload_count++;
}

static ERL_NIF_TERM increment(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int input;
    fixture_state *state = enif_priv_data(env);

    if (argc != 1 || !enif_get_int(env, argv[0], &input) || input == INT_MAX)
        return enif_make_badarg(env);

    state->successful_calls++;
    return enif_make_int(env, input + 1);
}

static ERL_NIF_TERM greeting(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argv;
    if (argc != 0)
        return enif_make_badarg(env);

    return enif_make_string(env, "native hello", ERL_NIF_LATIN1);
}

ERL_NAPI_EXPORT int memory_allocator_fixture_unload_count(void)
{
    return unload_count;
}

static ErlNifFunc functions[] = {
    {"increment", 1, increment, 0},
    {"greeting", 0, greeting, 0}
};

ERL_NIF_INIT(memory_allocator_nif_fixture, functions, load_fixture, NULL, NULL, unload_fixture)
