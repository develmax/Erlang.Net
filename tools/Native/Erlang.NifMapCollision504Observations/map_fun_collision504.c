#include "erl_nif.h"

enum { KEY_COUNT = 33, FUN_COUNT = 3, FILLER_COUNT = 31, SNAPSHOT_FIELDS = 8 };

static ERL_NIF_TERM integer(ErlNifEnv *env, int value)
{
    return enif_make_int(env, value);
}

static ERL_NIF_TERM tuple(ErlNifEnv *env, unsigned count, const ERL_NIF_TERM *values)
{
    return enif_make_tuple_from_array(env, values, count);
}

static ERL_NIF_TERM lookup(ErlNifEnv *env, ERL_NIF_TERM map, ERL_NIF_TERM key)
{
    ERL_NIF_TERM output = integer(env, -1), values[2];
    values[0] = integer(env, enif_get_map_value(env, map, key, &output));
    values[1] = output;
    return tuple(env, 2, values);
}

/* Counts and sums are public observer operations, independent of the own-VM
   atom word used by the local-fun hash. No captured ETF is normalized. */
static ERL_NIF_TERM snapshot(ErlNifEnv *env, ERL_NIF_TERM map, const ERL_NIF_TERM *functions)
{
    ERL_NIF_TERM fields[SNAPSHOT_FIELDS], key, value, counts[FUN_COUNT], sums[FUN_COUNT];
    ERL_NIF_TERM visits[FUN_COUNT + 1];
    unsigned visit_count = 0;
    ErlNifMapIterator iterator;
    size_t size = 0;
    int found[FUN_COUNT] = {0}, total[FUN_COUNT] = {0}, fillers = 0, filler_sum = 0, unknown = 0;
    int index, number, matched;
    if (!enif_get_map_size(env, map, &size) ||
        !enif_map_iterator_create(env, map, &iterator, ERL_NIF_MAP_ITERATOR_FIRST))
        return enif_make_badarg(env);
    while (enif_map_iterator_get_pair(env, &iterator, &key, &value)) {
        if (!enif_get_int(env, value, &number)) {
            enif_map_iterator_destroy(env, &iterator);
            return enif_make_badarg(env);
        }
        matched = 0;
        for (index = 0; index < FUN_COUNT; index++) {
            if (enif_is_identical(key, functions[index])) {
                if (visit_count == FUN_COUNT + 1) {
                    enif_map_iterator_destroy(env, &iterator);
                    return enif_make_badarg(env);
                }
                visits[visit_count++] = integer(env, index);
                found[index]++;
                total[index] += number;
                matched = 1;
                break;
            }
        }
        if (!matched) {
            if (enif_get_int(env, key, &index) && index >= 1 && index <= FILLER_COUNT) {
                fillers++;
                filler_sum += number;
            } else unknown++;
        }
        enif_map_iterator_next(env, &iterator);
    }
    enif_map_iterator_destroy(env, &iterator);
    for (index = 0; index < FUN_COUNT; index++) {
        counts[index] = integer(env, found[index]);
        sums[index] = integer(env, total[index]);
    }
    fields[0] = integer(env, (int)size);
    for (index = 0; index < FUN_COUNT; index++)
        fields[index + 1] = lookup(env, map, functions[index]);
    fields[4] = tuple(env, FUN_COUNT, counts);
    fields[5] = tuple(env, FUN_COUNT, sums);
    {
        ERL_NIF_TERM rest[3] = {integer(env, fillers), integer(env, filler_sum), integer(env, unknown)};
        fields[6] = tuple(env, 3, rest);
    }
    fields[7] = enif_make_list_from_array(env, visits, visit_count);
    return tuple(env, SNAPSHOT_FIELDS, fields);
}

static ERL_NIF_TERM operation(ErlNifEnv *env, ERL_NIF_TERM map, const ERL_NIF_TERM *functions,
                              int kind, int which)
{
    ERL_NIF_TERM sentinel = integer(env, -313), output = sentinel, fields[3];
    int status;
    if (kind == 0) status = enif_make_map_put(env, map, functions[which], integer(env, 500 + which), &output);
    else if (kind == 1) status = enif_make_map_update(env, map, functions[which], integer(env, 600 + which), &output);
    else status = enif_make_map_remove(env, map, functions[which], &output);
    fields[0] = integer(env, status);
    fields[1] = integer(env, output != sentinel);
    fields[2] = status ? snapshot(env, output, functions) : integer(env, -1);
    return tuple(env, 3, fields);
}

/* Roles are selected by the real native iterator before any mutation. This
   reads own-VM state; it does not rewrite or sort a captured result. */
static int select_roles(ErlNifEnv *env, ERL_NIF_TERM map, const ERL_NIF_TERM *input,
                         ERL_NIF_TERM *roles)
{
    ErlNifMapIterator iterator;
    ERL_NIF_TERM key, value;
    int which;
    if (!enif_map_iterator_create(env, map, &iterator, ERL_NIF_MAP_ITERATOR_FIRST))
        return 0;
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

static ERL_NIF_TERM run(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM keys[KEY_COUNT], values[KEY_COUNT], map, fields[12], identity[6], roles[FUN_COUNT];
    int index, kind, which;
    if (argc != FUN_COUNT) return enif_make_badarg(env);
    keys[0] = argv[0]; values[0] = integer(env, 100);
    keys[1] = argv[1]; values[1] = integer(env, 100);
    for (index = 0; index < FILLER_COUNT; index++) {
        keys[index + 2] = integer(env, index + 1);
        values[index + 2] = integer(env, 1000 + index);
    }
    if (!enif_make_map_from_arrays(env, keys, values, KEY_COUNT, &map))
        return enif_make_badarg(env);
    if (!select_roles(env, map, argv, roles)) return enif_make_badarg(env);
    identity[0] = integer(env, enif_is_identical(argv[0], argv[1]));
    identity[1] = integer(env, enif_compare(argv[0], argv[1]));
    identity[2] = integer(env, enif_is_identical(argv[0], argv[2]));
    identity[3] = integer(env, enif_compare(argv[0], argv[2]));
    identity[4] = integer(env, enif_is_identical(argv[1], argv[2]));
    identity[5] = integer(env, enif_compare(argv[1], argv[2]));
    fields[0] = tuple(env, 6, identity);
    fields[1] = snapshot(env, map, roles);
    for (kind = 0; kind < 3; kind++)
        for (which = 0; which < FUN_COUNT; which++)
            fields[2 + kind * FUN_COUNT + which] = operation(env, map, roles, kind, which);
    fields[11] = snapshot(env, map, roles);
    return tuple(env, 12, fields);
}

static ErlNifFunc functions[] = {{"run", 3, run, 0}};
ERL_NIF_INIT(map_fun_collision504, functions, NULL, NULL, NULL, NULL)
