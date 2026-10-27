#include "erl_nif.h"

typedef struct state557 {
    int marker;
    int initial_null;
    int first_visible;
    int second_visible;
} state557;

static int load557(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info) {
    (void)info;
    int initial_null = enif_priv_data(env) == NULL;
    state557 *first = enif_alloc(sizeof(*first));
    if (first == NULL) return 1;
    first->marker = 101;
    *private_data = first;
    state557 *visible = enif_priv_data(env);
    int first_visible = visible == NULL ? -1 : visible->marker;
    state557 *second = enif_alloc(sizeof(*second));
    if (second == NULL) {
        *private_data = NULL;
        enif_free(first);
        return 2;
    }
    second->marker = 202;
    *private_data = second;
    visible = enif_priv_data(env);
    second->initial_null = initial_null;
    second->first_visible = first_visible;
    second->second_visible = visible == NULL ? -1 : visible->marker;
    enif_free(first);
    return 0;
}

static void unload557(ErlNifEnv *env, void *private_data) {
    (void)env;
    enif_free(private_data);
}

static ERL_NIF_TERM read557(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    state557 *state = enif_priv_data(env);
    (void)argc; (void)argv;
    if (state == NULL) return enif_make_badarg(env);
    return enif_make_tuple4(env,
        enif_make_int(env, state->initial_null),
        enif_make_int(env, state->first_visible),
        enif_make_int(env, state->second_visible),
        enif_make_int(env, state->marker));
}

static ErlNifFunc functions557[] = {{"read", 0, read557, 0}};
ERL_NIF_INIT(load_private557, functions557, load557, NULL, NULL, unload557)
