#include "erl_nif.h"
#include "entry_fixture_names.h"

static ERL_NIF_TERM inspect_with_env(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[], ErlNifEnv *predicate_env)
{
    ERL_NIF_TERM values[TERM_PREDICATE_COUNT];
    ERL_NIF_TERM head = enif_make_int(env, TERM_SENTINEL_HEAD);
    ERL_NIF_TERM tail = enif_make_int(env, TERM_SENTINEL_TAIL);
    ERL_NIF_TERM cell[3], result[2];
    int flags[TERM_PREDICATE_COUNT];
    unsigned i;
    if (argc != 1)
        return enif_make_badarg(env);
    flags[0] = enif_is_atom(predicate_env, argv[0]);
    flags[1] = enif_is_binary(predicate_env, argv[0]);
    flags[2] = enif_is_ref(predicate_env, argv[0]);
    flags[3] = enif_is_fun(predicate_env, argv[0]);
    flags[4] = enif_is_pid(predicate_env, argv[0]);
    flags[5] = enif_is_port(predicate_env, argv[0]);
    flags[6] = enif_is_empty_list(predicate_env, argv[0]);
    flags[7] = enif_is_list(predicate_env, argv[0]);
    flags[8] = enif_is_tuple(predicate_env, argv[0]);
    flags[9] = enif_is_number(predicate_env, argv[0]);
    flags[10] = enif_is_map(predicate_env, argv[0]);
    for (i = 0; i < TERM_PREDICATE_COUNT; ++i)
        values[i] = enif_make_int(env, flags[i]);
    cell[0] = enif_make_int(env, enif_get_list_cell(predicate_env, argv[0], &head, &tail));
    cell[1] = head;
    cell[2] = tail;
    result[0] = enif_make_tuple_from_array(env, values, TERM_PREDICATE_COUNT);
    result[1] = enif_make_tuple_from_array(env, cell, 3);
    return enif_make_tuple_from_array(env, result, 2);
}

static ERL_NIF_TERM inspect(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    return inspect_with_env(env, argc, argv, env);
}

static ERL_NIF_TERM inspect_null(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    return inspect_with_env(env, argc, argv, NULL);
}

static ERL_NIF_TERM arrays(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int count;
    unsigned i;
    ERL_NIF_TERM *values = NULL;
    ERL_NIF_TERM tuple, list, result[2];
    if (argc != 1 || !enif_get_int(env, argv[0], &count)
        || (count != 0 && count != 3 && count != TERM_ARRAY_LARGE_COUNT))
        return enif_make_badarg(env);
    if (count != 0) {
        values = enif_alloc((size_t)count * sizeof(*values));
        if (values == NULL)
            return enif_make_badarg(env);
        for (i = 0; i < (unsigned)count; ++i)
            values[i] = enif_make_int(env, (int)i);
    }
    tuple = enif_make_tuple_from_array(env, values, (unsigned)count);
    list = enif_make_list_from_array(env, values, (unsigned)count);
    if (values != NULL) {
        values[0] = enif_make_int(env, -1);
        enif_free(values);
    }
    result[0] = tuple;
    result[1] = list;
    return enif_make_tuple_from_array(env, result, 2);
}

static ErlNifFunc functions[] = {
    {"inspect", 1, inspect, 0},
    {"inspect_null", 1, inspect_null, 0},
    {"arrays", 1, arrays, 0}
};

ERL_NIF_INIT(term_callback_contract, functions, NULL, NULL, NULL, NULL)
