#include "erl_nif.h"
#include <string.h>

enum { MAX_KEYS = 65, BINARY_BYTES = 65 };

static ERL_NIF_TERM make(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM keys[MAX_KEYS], values[MAX_KEYS], map, fields[3];
    unsigned char *data, *mutable_bytes = NULL;
    int count, length, target, index;
    if (argc != 3 || !enif_get_int(env, argv[0], &count) ||
        !enif_get_int(env, argv[1], &length) || !enif_get_int(env, argv[2], &target) ||
        count < 2 || count > MAX_KEYS || (length != 64 && length != BINARY_BYTES) ||
        target < 0 || target > 255)
        return enif_make_badarg(env);
    for (index = 0; index < count; index++) {
        data = enif_make_new_binary(env, (size_t)length, &keys[index]);
        if (data == NULL) return enif_make_badarg(env);
        memset(data, index, (size_t)length);
        if (index == 0) mutable_bytes = data;
        values[index] = enif_make_int(env, 100 + index);
    }
    if (!enif_make_map_from_arrays(env, keys, values, (size_t)count, &map))
        return enif_make_badarg(env);
    data = enif_make_new_binary(env, (size_t)length, &fields[1]);
    if (data == NULL) return enif_make_badarg(env);
    memset(data, target, (size_t)length);
    data = enif_make_new_binary(env, (size_t)length, &fields[2]);
    if (data == NULL) return enif_make_badarg(env);
    memset(data, 0, (size_t)length);
    memset(mutable_bytes, target, (size_t)length);
    fields[0] = map;
    return enif_make_tuple_from_array(env, fields, 3);
}

static ErlNifFunc functions[] = {{"make", 3, make, 0}};
ERL_NIF_INIT(map_postreturn505, functions, NULL, NULL, NULL, NULL)
