#include "erl_nif.h"

enum { ORIGINAL_KEYS = 33, NUMERIC_KEYS = 31, ROLES = 3, TARGET_CAPACITY = 3 };

static ERL_NIF_TERM number(ErlNifEnv *env, int value)
{
    return enif_make_int(env, value);
}

static ERL_NIF_TERM tuple(ErlNifEnv *env, unsigned count, const ERL_NIF_TERM *fields)
{
    return enif_make_tuple_from_array(env, fields, count);
}

static int select_roles(ErlNifEnv *env, ERL_NIF_TERM map, const ERL_NIF_TERM *input,
                         ERL_NIF_TERM *roles)
{
    ErlNifMapIterator iterator;
    ERL_NIF_TERM key, value;
    int which;
    if (!enif_map_iterator_create(env, map, &iterator, ERL_NIF_MAP_ITERATOR_FIRST)) return 0;
    while (enif_map_iterator_get_pair(env, &iterator, &key, &value)) {
        for (which = 0; which < 2; which++) {
            if (enif_is_identical(key, input[which])) {
                roles[0] = input[which];
                roles[1] = input[1 - which];
                roles[2] = input[2];
                enif_map_iterator_destroy(env, &iterator);
                return 1;
            }
        }
        enif_map_iterator_next(env, &iterator);
    }
    enif_map_iterator_destroy(env, &iterator);
    return 0;
}

static ERL_NIF_TERM build(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM keys[ORIGINAL_KEYS], values[ORIGINAL_KEYS], map, duplicate, flat;
    ERL_NIF_TERM roles[ROLES], result[4];
    int index;
    if (argc != ROLES) return enif_make_badarg(env);
    keys[0] = argv[0]; values[0] = number(env, 100);
    keys[1] = argv[1]; values[1] = number(env, 100);
    for (index = 0; index < NUMERIC_KEYS; index++) {
        keys[index + 2] = number(env, index + 1);
        values[index + 2] = number(env, 1000 + index);
    }
    if (!enif_make_map_from_arrays(env, keys, values, ORIGINAL_KEYS, &map) ||
        !select_roles(env, map, argv, roles) ||
        !enif_make_map_put(env, map, roles[1], number(env, 501), &duplicate) ||
        !enif_make_map_remove(env, duplicate, number(env, 1), &flat))
        return enif_make_badarg(env);
    /* Only the unique numeric filler is removed while converting33->32.
       Both same-word fun keys survive; every allocated flat slot is filled. */
    result[0] = flat;
    for (index = 0; index < ROLES; index++) result[index + 1] = roles[index];
    return tuple(env, 4, result);
}

static ERL_NIF_TERM lookup(ErlNifEnv *env, ERL_NIF_TERM map, ERL_NIF_TERM key)
{
    ERL_NIF_TERM output = number(env, -1), result[2];
    result[0] = number(env, enif_get_map_value(env, map, key, &output));
    result[1] = output;
    return tuple(env, 2, result);
}

static ERL_NIF_TERM snapshot(ErlNifEnv *env, ERL_NIF_TERM map, const ERL_NIF_TERM *roles)
{
    ErlNifMapIterator iterator;
    ERL_NIF_TERM fields[6], key, value, target[TARGET_CAPACITY], role_values[ROLES];
    ERL_NIF_TERM numeric_queries[NUMERIC_KEYS], iterator_values[NUMERIC_KEYS];
    int seen[NUMERIC_KEYS] = {0}, stored[NUMERIC_KEYS], which, int_value;
    int unknown = 0, repeated_numeric = 0, total = 0;
    unsigned targets = 0;
    size_t size = 0;
    if (!enif_get_map_size(env, map, &size) ||
        !enif_map_iterator_create(env, map, &iterator, ERL_NIF_MAP_ITERATOR_FIRST))
        return enif_make_badarg(env);
    for (which = 0; which < NUMERIC_KEYS; which++) stored[which] = -1;
    while (enif_map_iterator_get_pair(env, &iterator, &key, &value)) {
        total++;
        if (enif_is_identical(key, roles[1])) {
            if (targets == TARGET_CAPACITY) {
                enif_map_iterator_destroy(env, &iterator);
                return enif_make_badarg(env);
            }
            target[targets++] = value;
        } else if (enif_get_int(env, key, &which) && which >= 1 && which <= NUMERIC_KEYS &&
                   enif_get_int(env, value, &int_value)) {
            repeated_numeric += seen[which - 1] != 0;
            seen[which - 1]++;
            stored[which - 1] = int_value;
        } else unknown++;
        enif_map_iterator_next(env, &iterator);
    }
    enif_map_iterator_destroy(env, &iterator);
    for (which = 0; which < ROLES; which++) role_values[which] = lookup(env, map, roles[which]);
    for (which = 0; which < NUMERIC_KEYS; which++) {
        numeric_queries[which] = lookup(env, map, number(env, which + 1));
        iterator_values[which] = number(env, stored[which]);
    }
    fields[0] = number(env, (int)size);
    fields[1] = tuple(env, ROLES, role_values);
    fields[2] = enif_make_list_from_array(env, target, targets);
    fields[3] = enif_make_list_from_array(env, numeric_queries, NUMERIC_KEYS);
    fields[4] = enif_make_list_from_array(env, iterator_values, NUMERIC_KEYS);
    {
        ERL_NIF_TERM counts[3] = {number(env, total), number(env, repeated_numeric), number(env, unknown)};
        fields[5] = tuple(env, 3, counts);
    }
    return tuple(env, 6, fields);
}

static ERL_NIF_TERM observe(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM result[7], changed, independent, copied;
    ErlNifEnv *other;
    if (argc != 4) return enif_make_badarg(env);
    result[0] = snapshot(env, argv[0], argv + 1);
    if (!enif_make_map_update(env, argv[0], argv[2], number(env, 701), &changed)) return enif_make_badarg(env);
    result[1] = snapshot(env, changed, argv + 1);
    if (!enif_make_map_put(env, argv[0], argv[2], number(env, 801), &changed)) return enif_make_badarg(env);
    result[2] = snapshot(env, changed, argv + 1);
    /* Duplicate-key deletion is now FLAT32->FLAT31, never HAMT33->FLAT32. */
    if (!enif_make_map_remove(env, argv[0], argv[2], &changed)) return enif_make_badarg(env);
    result[3] = snapshot(env, changed, argv + 1);
    if (!enif_make_map_remove(env, argv[0], number(env, 1), &changed)) return enif_make_badarg(env);
    result[4] = snapshot(env, changed, argv + 1);
    other = enif_alloc_env();
    if (other == NULL) return enif_make_badarg(env);
    independent = enif_make_copy(other, argv[0]);
    copied = enif_make_copy(env, independent);
    enif_clear_env(other);
    enif_free_env(other);
    result[5] = snapshot(env, copied, argv + 1);
    result[6] = snapshot(env, argv[0], argv + 1);
    return tuple(env, 7, result);
}

static ErlNifFunc functions[] = {{"build", 3, build, 0}, {"observe", 4, observe, 0}};
ERL_NIF_INIT(map_reentry505, functions, NULL, NULL, NULL, NULL)
