#include "erl_nif.h"

static ERL_NIF_TERM empty(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc;
    (void)argv;
    return enif_make_new_map(env);
}

static ERL_NIF_TERM observe(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM result[8], pair[2], first, second, independent_map, copied;
    ErlNifEnv *independent;
    size_t first_size = (size_t)-1, copied_size = (size_t)-1;
    int first_status, copied_status;
    (void)argc;
    (void)argv;
    first = enif_make_new_map(env);
    second = enif_make_new_map(env);
    first_status = enif_get_map_size(env, first, &first_size);
    independent = enif_alloc_env();
    if (independent == NULL) return enif_make_badarg(env);
    independent_map = enif_make_new_map(independent);
    copied = enif_make_copy(env, independent_map);
    enif_clear_env(independent);
    enif_free_env(independent);
    copied_status = enif_get_map_size(env, copied, &copied_size);
    result[0] = first;
    result[1] = second;
    result[2] = enif_make_int(env, enif_is_identical(first, second));
    result[3] = enif_make_int(env, enif_is_map(env, first));
    result[4] = enif_make_int(env, first_status);
    result[5] = enif_make_uint64(env, (ErlNifUInt64)first_size);
    result[6] = copied;
    pair[0] = enif_make_int(env, copied_status);
    pair[1] = enif_make_uint64(env, (ErlNifUInt64)copied_size);
    result[7] = enif_make_tuple_from_array(env, pair, 2);
    return enif_make_tuple_from_array(env, result, 8);
}

static ErlNifFunc functions[] = {{"empty", 0, empty, 0}, {"observe", 0, observe, 0}};
ERL_NIF_INIT(map_creation501, functions, NULL, NULL, NULL, NULL)
