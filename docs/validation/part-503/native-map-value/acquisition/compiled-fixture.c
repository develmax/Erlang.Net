#include "erl_nif.h"

static ERL_NIF_TERM pair(ErlNifEnv *env, int status, ERL_NIF_TERM value)
{
    ERL_NIF_TERM fields[2] = {enif_make_int(env, status), value};
    return enif_make_tuple_from_array(env, fields, 2);
}

static ERL_NIF_TERM query(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM storage[3], fields[4];
    ErlNifEnv *hint = env, *independent = NULL;
    int mode, status;
    if (argc != 3 || !enif_get_int(env, argv[0], &mode) || mode < 0 || mode > 3)
        return enif_make_badarg(env);
    storage[0] = enif_make_int(env, 17);
    storage[1] = enif_make_atom(env, "unchanged");
    storage[2] = enif_make_int(env, 29);
    if (mode == 1) hint = NULL;
    if (mode == 2) {
        independent = enif_alloc_env();
        if (independent == NULL) return enif_make_badarg(env);
        hint = independent;
    }
    status = enif_get_map_value(hint, argv[1], argv[2], mode == 3 ? NULL : &storage[1]);
    if (independent != NULL) enif_free_env(independent);
    fields[0] = enif_make_int(env, status);
    fields[1] = storage[1];
    fields[2] = storage[0];
    fields[3] = storage[2];
    return enif_make_tuple_from_array(env, fields, 4);
}

static ERL_NIF_TERM independent(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ErlNifEnv *owner;
    ERL_NIF_TERM map, output, copied;
    int status;
    if (argc != 2) return enif_make_badarg(env);
    owner = enif_alloc_env();
    if (owner == NULL) return enif_make_badarg(env);
    map = enif_make_copy(owner, argv[0]);
    output = enif_make_atom(env, "unchanged");
    status = enif_get_map_value(NULL, map, argv[1], &output);
    copied = status ? enif_make_copy(env, output) : output;
    enif_clear_env(owner);
    enif_free_env(owner);
    return pair(env, status, copied);
}

static ERL_NIF_TERM pending_binary_key(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM key, first, second, fields[2];
    unsigned char *bytes;
    int first_status, second_status;
    if (argc != 1) return enif_make_badarg(env);
    bytes = enif_make_new_binary(env, 2, &key);
    if (bytes == NULL) return enif_make_badarg(env);
    first = second = enif_make_atom(env, "unchanged");
    bytes[0] = 1; bytes[1] = 2;
    first_status = enif_get_map_value(env, argv[0], key, &first);
    bytes[0] = 3; bytes[1] = 4;
    second_status = enif_get_map_value(env, argv[0], key, &second);
    fields[0] = pair(env, first_status, first);
    fields[1] = pair(env, second_status, second);
    return enif_make_tuple_from_array(env, fields, 2);
}

static ErlNifFunc functions[] = {
    {"query", 3, query, 0},
    {"independent", 2, independent, 0},
    {"pending_binary_key", 1, pending_binary_key, 0}
};
ERL_NIF_INIT(map_value503, functions, NULL, NULL, NULL, NULL)
