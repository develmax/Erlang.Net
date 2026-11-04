#include "erl_nif.h"
static int loads;

static int load(ErlNifEnv *env, void **data, ERL_NIF_TERM info)
{
    int *state;
    (void)env; (void)info;
    state = enif_alloc(sizeof(int));
    if (state == NULL) return 1;
    *state = ++loads;
    *data = state;
    return 0;
}

static void unload(ErlNifEnv *env, void *data)
{
    (void)env;
    enif_free(data);
}

static ERL_NIF_TERM sum(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int left, right;
    if (argc != 2 || !enif_get_int(env, argv[0], &left) || !enif_get_int(env, argv[1], &right))
        return enif_make_badarg(env);
    return enif_make_int(env, left + right);
}

static ERL_NIF_TERM tuple(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM values[17];
    int i;
    (void)argc; (void)argv;
    for (i = 0; i < 17; i++) values[i] = enif_make_int(env, i + 1);
    return enif_make_tuple(env, 17,
        values[0], values[1], values[2], values[3], values[4], values[5],
        values[6], values[7], values[8], values[9], values[10], values[11],
        values[12], values[13], values[14], values[15], values[16]);
}

static ERL_NIF_TERM generation(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_int(env, *(int *)enif_priv_data(env));
}

static ErlNifFunc functions[] = {
    {"sum", 2, sum, 0}, {"tuple", 0, tuple, 0}, {"generation", 0, generation, 0}
};
ERL_NIF_INIT(nif_platform_fixture, functions, load, NULL, NULL, unload)
