#include "erl_nif.h"
#include <string.h>

static ERL_NIF_TERM tuple(ErlNifEnv *env, unsigned count, const ERL_NIF_TERM *values)
{
    return enif_make_tuple_from_array(env, values, count);
}

static ERL_NIF_TERM pair(ErlNifEnv *env, ERL_NIF_TERM a, ERL_NIF_TERM b)
{
    ERL_NIF_TERM fields[2] = {a, b};
    return tuple(env, 2, fields);
}

static ERL_NIF_TERM shape(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ErlNifEnv *hint = env, *other = NULL;
    ERL_NIF_TERM fields[5], missing, missing_key;
    size_t size = 777;
    int mode, status, map, missing_status;
    if (argc != 2 || !enif_get_int(env, argv[0], &mode) || mode < 0 || mode > 2)
        return enif_make_badarg(env);
    if (mode == 1) hint = NULL;
    if (mode == 2) {
        other = enif_alloc_env();
        if (other == NULL) return enif_make_badarg(env);
        hint = other;
    }
    map = enif_is_map(hint, argv[1]);
    status = enif_get_map_size(hint, argv[1], &size);
    missing = enif_make_atom(env, "unchanged");
    missing_key = enif_make_atom(env, "map_family504_absent_key");
    missing_status = enif_get_map_value(hint, argv[1], missing_key, &missing);
    if (other != NULL) enif_free_env(other);
    fields[0] = enif_make_int(env, map);
    fields[1] = enif_make_int(env, status);
    fields[2] = enif_make_int(env, (int)size);
    fields[3] = enif_make_int(env, missing_status);
    fields[4] = missing;
    return tuple(env, 5, fields);
}

static ERL_NIF_TERM mutate(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM fields[5], words[3], before;
    int mode, status;
    if (argc != 4 || !enif_get_int(env, argv[0], &mode) || mode < 0 || mode > 2)
        return enif_make_badarg(env);
    before = enif_make_atom(env, "unchanged");
    words[0] = enif_make_int(env, 17);
    words[1] = before;
    words[2] = enif_make_int(env, 29);
    if (mode == 0) status = enif_make_map_put(env, argv[1], argv[2], argv[3], &words[1]);
    else if (mode == 1) status = enif_make_map_update(env, argv[1], argv[2], argv[3], &words[1]);
    else status = enif_make_map_remove(env, argv[1], argv[2], &words[1]);
    fields[0] = enif_make_int(env, status);
    fields[1] = enif_make_int(env, words[1] != before);
    fields[2] = status ? words[1] : enif_make_atom(env, "not_returned");
    fields[3] = words[0];
    fields[4] = words[2];
    return tuple(env, 5, fields);
}

static ERL_NIF_TERM nonmap_null(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM fields[3];
    if (argc != 1 || enif_is_map(env, argv[0])) return enif_make_badarg(env);
    fields[0] = enif_make_int(env, enif_make_map_put(NULL, argv[0], argv[0], argv[0], NULL));
    fields[1] = enif_make_int(env, enif_make_map_update(NULL, argv[0], argv[0], argv[0], NULL));
    fields[2] = enif_make_int(env, enif_make_map_remove(NULL, argv[0], argv[0], NULL));
    return tuple(env, 3, fields);
}

static ERL_NIF_TERM from_arrays(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    unsigned count, index;
    ERL_NIF_TERM *keys = NULL, *values = NULL, list, head, tail, result, sentinel, fields[3];
    int arity, status;
    const ERL_NIF_TERM *items;
    if (argc != 1 || !enif_get_list_length(env, argv[0], &count) || count > 4096)
        return enif_make_badarg(env);
    if (count != 0) {
        keys = enif_alloc(sizeof(ERL_NIF_TERM) * count);
        values = enif_alloc(sizeof(ERL_NIF_TERM) * count);
        if (keys == NULL || values == NULL) {
            if (keys != NULL) enif_free(keys);
            if (values != NULL) enif_free(values);
            return enif_make_badarg(env);
        }
    }
    list = argv[0];
    for (index = 0; index < count; index++) {
        if (!enif_get_list_cell(env, list, &head, &tail) || !enif_get_tuple(env, head, &arity, &items) || arity != 2) {
            enif_free(keys);
            enif_free(values);
            return enif_make_badarg(env);
        }
        keys[index] = items[0];
        values[index] = items[1];
        list = tail;
    }
    sentinel = enif_make_atom(env, "unchanged");
    result = sentinel;
    status = enif_make_map_from_arrays(env, keys, values, count, &result);
    if (keys != NULL) enif_free(keys);
    if (values != NULL) enif_free(values);
    fields[0] = enif_make_int(env, status);
    fields[1] = enif_make_int(env, result != sentinel);
    /* Failure may write THE_NON_VALUE; never turn that invalid word into a term. */
    fields[2] = status ? result : enif_make_atom(env, "not_returned");
    return tuple(env, 3, fields);
}

static ERL_NIF_TERM iterator_snapshot(ErlNifEnv *env, ErlNifEnv *hint, ErlNifMapIterator *iter)
{
    ERL_NIF_TERM fields[5], key = enif_make_atom(env, "unchanged_key"), value = enif_make_atom(env, "unchanged_value");
    int got = enif_map_iterator_get_pair(hint, iter, &key, &value);
    fields[0] = enif_make_int(env, enif_map_iterator_is_head(hint, iter));
    fields[1] = enif_make_int(env, enif_map_iterator_is_tail(hint, iter));
    fields[2] = enif_make_int(env, got);
    fields[3] = key;
    fields[4] = value;
    return tuple(env, 5, fields);
}

static int move(ErlNifEnv *env, ErlNifMapIterator *iter, int first)
{
    return first ? enif_map_iterator_next(env, iter) : enif_map_iterator_prev(env, iter);
}

static ERL_NIF_TERM iterator(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ErlNifMapIterator iter;
    ErlNifEnv *hint = env, *other = NULL;
    ERL_NIF_TERM fields[10], *visited, key, value;
    size_t size = 0, count = 0;
    int encoded_direction, direction, hint_mode, created, first, repeated, back, forward;
    if (argc != 2 || !enif_get_int(env, argv[0], &encoded_direction)) return enif_make_badarg(env);
    hint_mode = encoded_direction / 10;
    direction = encoded_direction % 10;
    if (hint_mode < 0 || hint_mode > 2) return enif_make_badarg(env);
    if (hint_mode == 1) hint = NULL;
    if (hint_mode == 2) {
        other = enif_alloc_env();
        if (other == NULL) return enif_make_badarg(env);
        hint = other;
    }
    if (enif_get_map_size(env, argv[1], &size) && size > 4096) {
        if (other != NULL) enif_free_env(other);
        return enif_make_badarg(env);
    }
    /* Invalid direction on a HAMT allocates a private stack before refusing; do not leak it. */
    if (size > 32 && direction != ERL_NIF_MAP_ITERATOR_FIRST && direction != ERL_NIF_MAP_ITERATOR_LAST) {
        if (other != NULL) enif_free_env(other);
        return enif_make_badarg(env);
    }
    memset(&iter, 0, sizeof(iter));
    created = enif_map_iterator_create(hint, argv[1], &iter, (ErlNifMapIteratorEntry)direction);
    if (!created) {
        if (other != NULL) enif_free_env(other);
        return pair(env, enif_make_int(env, 0), enif_make_atom(env, "not_created"));
    }
    visited = enif_alloc(sizeof(ERL_NIF_TERM) * (size + 1));
    if (visited == NULL) {
        enif_map_iterator_destroy(hint, &iter);
        if (other != NULL) enif_free_env(other);
        return enif_make_badarg(env);
    }
    first = direction == ERL_NIF_MAP_ITERATOR_FIRST;
    fields[0] = enif_make_int(env, created);
    fields[1] = iterator_snapshot(env, hint, &iter);
    while (count < size && enif_map_iterator_get_pair(hint, &iter, &key, &value)) {
        visited[count++] = pair(env, key, value);
        if (!move(hint, &iter, first)) break;
    }
    fields[2] = enif_make_list_from_array(env, visited, (unsigned)count);
    fields[3] = iterator_snapshot(env, hint, &iter);
    if (!enif_map_iterator_get_pair(hint, &iter, &key, &value))
        fields[9] = enif_make_int(env, enif_map_iterator_get_pair(hint, &iter, NULL, NULL));
    else fields[9] = enif_make_atom(env, "not_at_boundary");
    repeated = move(hint, &iter, first);
    fields[4] = enif_make_int(env, repeated);
    fields[5] = iterator_snapshot(env, hint, &iter);
    back = move(hint, &iter, !first);
    fields[6] = pair(env, enif_make_int(env, back), iterator_snapshot(env, hint, &iter));
    forward = move(hint, &iter, first);
    fields[7] = pair(env, enif_make_int(env, forward), iterator_snapshot(env, hint, &iter));
    enif_map_iterator_destroy(hint, &iter);
    fields[8] = enif_make_int(env, enif_is_map(env, argv[1]));
    enif_free(visited);
    if (other != NULL) enif_free_env(other);
    return tuple(env, 10, fields);
}

static ERL_NIF_TERM pending_value(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM binary, map, borrowed, wrapped, key, fields[5], keys[1], values[1], iterated;
    ErlNifMapIterator iter;
    unsigned char *bytes;
    int size, mode, status;
    if (argc != 2 || !enif_get_int(env, argv[0], &size) || size < 1 || size > 4096 ||
        !enif_get_int(env, argv[1], &mode) || mode < 0 || mode > 2)
        return enif_make_badarg(env);
    bytes = enif_make_new_binary(env, (size_t)size, &binary);
    if (bytes == NULL) return enif_make_badarg(env);
    memset(bytes, 17, (size_t)size);
    key = enif_make_atom(env, "value");
    map = enif_make_new_map(env);
    if (mode == 0) status = enif_make_map_put(env, map, key, binary, &map);
    else if (mode == 1) {
        if (!enif_make_map_put(env, map, key, enif_make_atom(env, "old"), &map)) return enif_make_badarg(env);
        status = enif_make_map_update(env, map, key, binary, &map);
    } else {
        keys[0] = key; values[0] = binary;
        status = enif_make_map_from_arrays(env, keys, values, 1, &map);
    }
    if (!status || !enif_get_map_value(env, map, key, &borrowed)) return enif_make_badarg(env);
    wrapped = pair(env, borrowed, map);
    if (!enif_map_iterator_create(env, map, &iter, ERL_NIF_MAP_ITERATOR_FIRST)) return enif_make_badarg(env);
    iterated = iterator_snapshot(env, env, &iter);
    enif_map_iterator_destroy(env, &iter);
    memset(bytes, 29, (size_t)size);
    fields[0] = enif_make_int(env, status);
    fields[1] = map;
    fields[2] = borrowed;
    fields[3] = wrapped;
    fields[4] = iterated;
    return tuple(env, 5, fields);
}

static ERL_NIF_TERM copy_boundary(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ErlNifEnv *other;
    ERL_NIF_TERM binary, map, key, copied, back, borrowed, fields[5];
    unsigned char *bytes;
    int size;
    if (argc != 1 || !enif_get_int(env, argv[0], &size) || size < 1 || size > 4096)
        return enif_make_badarg(env);
    bytes = enif_make_new_binary(env, (size_t)size, &binary);
    if (bytes == NULL) return enif_make_badarg(env);
    memset(bytes, 17, (size_t)size);
    key = enif_make_atom(env, "value");
    map = enif_make_new_map(env);
    if (!enif_make_map_put(env, map, key, binary, &map) || !enif_get_map_value(env, map, key, &borrowed))
        return enif_make_badarg(env);
    other = enif_alloc_env();
    if (other == NULL) return enif_make_badarg(env);
    copied = enif_make_copy(other, map);
    back = enif_make_copy(env, copied);
    enif_clear_env(other);
    enif_free_env(other);
    memset(bytes, 29, (size_t)size);
    fields[0] = enif_make_int(env, size);
    fields[1] = map;
    fields[2] = borrowed;
    fields[3] = back;
    fields[4] = pair(env, binary, back);
    return tuple(env, 5, fields);
}

static ERL_NIF_TERM pending_key(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM initial, replaced, argument, stored, inserted, first, second, fields[5];
    ERL_NIF_TERM key = enif_make_atom(env, "key"), value = enif_make_atom(env, "value");
    unsigned char *bytes, *stored_bytes;
    int found;
    (void)argv;
    if (argc != 0) return enif_make_badarg(env);
    bytes = enif_make_new_binary(env, 2, &argument);
    stored_bytes = enif_make_new_binary(env, 2, &stored);
    if (bytes == NULL || stored_bytes == NULL) return enif_make_badarg(env);
    bytes[0] = stored_bytes[0] = 1; bytes[1] = stored_bytes[1] = 2;
    initial = enif_make_new_map(env);
    /* A separately copied immutable key is essential: only the temporary argument changes. */
    {
        ErlNifBinary immutable;
        if (!enif_alloc_binary(2, &immutable)) return enif_make_badarg(env);
        immutable.data[0] = 1; immutable.data[1] = 2;
        first = enif_make_binary(env, &immutable);
    }
    if (!enif_make_map_put(env, initial, first, key, &initial) ||
        !enif_make_map_put(env, initial, argument, value, &replaced)) return enif_make_badarg(env);
    bytes[0] = 3; bytes[1] = 4;
    inserted = enif_make_new_map(env);
    if (!enif_make_map_put(env, inserted, stored, key, &inserted)) return enif_make_badarg(env);
    stored_bytes[0] = 5; stored_bytes[1] = 6;
    second = enif_make_atom(env, "unchanged");
    fields[0] = initial;
    fields[1] = replaced;
    fields[2] = argument;
    fields[3] = inserted;
    found = enif_get_map_value(env, inserted, stored, &second);
    fields[4] = pair(env, enif_make_int(env, found), second);
    return tuple(env, 5, fields);
}

static ERL_NIF_TERM independent(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ErlNifEnv *owner;
    ERL_NIF_TERM copied, result, key, value, back, borrowed, fields[3];
    if (argc != 1 || !enif_is_map(env, argv[0])) return enif_make_badarg(env);
    owner = enif_alloc_env();
    if (owner == NULL) return enif_make_badarg(env);
    copied = enif_make_copy(owner, argv[0]);
    key = enif_make_atom(owner, "added"); value = enif_make_int(owner, 99);
    if (!enif_make_map_put(owner, copied, key, value, &result) || !enif_get_map_value(NULL, result, key, &borrowed)) {
        enif_free_env(owner);
        return enif_make_badarg(env);
    }
    back = enif_make_copy(env, result);
    fields[0] = enif_make_copy(env, borrowed);
    fields[1] = back;
    fields[2] = argv[0];
    enif_clear_env(owner);
    enif_free_env(owner);
    return tuple(env, 3, fields);
}

static ERL_NIF_TERM alias_output(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM original, map, key, value, fields[5];
    int put_status, update_status, remove_status;
    ERL_NIF_TERM statuses[3];
    if (argc != 3) return enif_make_badarg(env);
    original = map = argv[0]; key = argv[1]; value = argv[2];
    put_status = enif_make_map_put(env, map, key, value, &map);
    if (!put_status) return enif_make_badarg(env);
    fields[0] = original;
    fields[1] = map;
    update_status = enif_make_map_update(env, map, key, enif_make_atom(env, "again"), &map);
    if (!update_status) return enif_make_badarg(env);
    fields[2] = map;
    remove_status = enif_make_map_remove(env, map, key, &map);
    if (!remove_status) return enif_make_badarg(env);
    fields[3] = map;
    statuses[0] = enif_make_int(env, put_status);
    statuses[1] = enif_make_int(env, update_status);
    statuses[2] = enif_make_int(env, remove_status);
    fields[4] = tuple(env, 3, statuses);
    return tuple(env, 5, fields);
}

static ERL_NIF_TERM identity(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM map = enif_make_new_map(env), result, observed, fields[3];
    int index;
    size_t size;
    if (argc != 3) return enif_make_badarg(env);
    for (index = 0; index < 3; index++) {
        if (!enif_make_map_put(env, map, argv[index], enif_make_int(env, index + 1), &map)) return enif_make_badarg(env);
    }
    for (index = 0; index < 3; index++) {
        int found = enif_get_map_value(env, map, argv[index], &observed);
        if (!found) return enif_make_badarg(env);
        result = enif_make_new_map(env);
        if (!enif_make_map_remove(env, map, argv[index], &result) || !enif_get_map_size(env, result, &size))
            return enif_make_badarg(env);
        fields[index] = pair(env, observed, enif_make_int(env, (int)size));
    }
    return tuple(env, 3, fields);
}

enum { mutablekey_MAX_KEYS = 65, mutablekey_MAX_BYTES = 65, mutablekey_RESULT_FIELDS = 12 };

static ERL_NIF_TERM mutablekey_number(ErlNifEnv *env, int value)
{
    return enif_make_int(env, value);
}

static ERL_NIF_TERM mutablekey_tuple(ErlNifEnv *env, unsigned count, const ERL_NIF_TERM *items)
{
    return enif_make_tuple_from_array(env, items, count);
}

static ERL_NIF_TERM mutablekey_lookup(ErlNifEnv *env, ERL_NIF_TERM map, ERL_NIF_TERM key)
{
    ERL_NIF_TERM output = mutablekey_number(env, -1), fields[2];
    int found = enif_get_map_value(env, map, key, &output);
    fields[0] = mutablekey_number(env, found);
    fields[1] = output;
    return mutablekey_tuple(env, 2, fields);
}

/* Return scalars, never a map with mutated structural keys. */
static ERL_NIF_TERM mutablekey_snapshot(ErlNifEnv *env, ERL_NIF_TERM map,
                             ERL_NIF_TERM current, ERL_NIF_TERM previous)
{
    ErlNifMapIterator iterator;
    ERL_NIF_TERM keys[mutablekey_MAX_KEYS + 2], key, value, fields[4];
    ErlNifBinary bytes;
    size_t size = 0;
    unsigned count = 0;
    if (!enif_get_map_size(env, map, &size) ||
        !enif_map_iterator_create(env, map, &iterator, ERL_NIF_MAP_ITERATOR_FIRST))
        return enif_make_badarg(env);
    while (enif_map_iterator_get_pair(env, &iterator, &key, &value)) {
        ERL_NIF_TERM entry[2];
        if (count == mutablekey_MAX_KEYS + 2 || !enif_inspect_binary(env, key, &bytes) || bytes.size == 0) {
            enif_map_iterator_destroy(env, &iterator);
            return enif_make_badarg(env);
        }
        entry[0] = mutablekey_number(env, (int)bytes.data[0]);
        entry[1] = value;
        keys[count++] = mutablekey_tuple(env, 2, entry);
        enif_map_iterator_next(env, &iterator);
    }
    enif_map_iterator_destroy(env, &iterator);
    fields[0] = mutablekey_number(env, (int)size);
    fields[1] = mutablekey_lookup(env, map, current);
    fields[2] = mutablekey_lookup(env, map, previous);
    fields[3] = enif_make_list_from_array(env, keys, count);
    return mutablekey_tuple(env, 4, fields);
}

static ERL_NIF_TERM mutablekey_run_case(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM keys[mutablekey_MAX_KEYS], values[mutablekey_MAX_KEYS], map, current, previous;
    ERL_NIF_TERM fields[mutablekey_RESULT_FIELDS], before_copy, copied_back, after_copy;
    ERL_NIF_TERM updated, removed, inserted, sentinel, singleton;
    ErlNifEnv *other;
    unsigned char *mutable_bytes = NULL, *data;
    int count, length, target, index, status;
    if (argc != 3 || !enif_get_int(env, argv[0], &count) ||
        !enif_get_int(env, argv[1], &length) || !enif_get_int(env, argv[2], &target) ||
        count < 2 || count > mutablekey_MAX_KEYS || (length != 64 && length != mutablekey_MAX_BYTES) ||
        target < 0 || target > 255)
        return enif_make_badarg(env);
    for (index = 0; index < count; index++) {
        data = enif_make_new_binary(env, (size_t)length, &keys[index]);
        if (data == NULL) return enif_make_badarg(env);
        memset(data, index, (size_t)length);
        if (index == 0) mutable_bytes = data;
        values[index] = mutablekey_number(env, 100 + index);
    }
    if (!enif_make_map_from_arrays(env, keys, values, (size_t)count, &map))
        return enif_make_badarg(env);
    data = enif_make_new_binary(env, (size_t)length, &current);
    if (data == NULL) return enif_make_badarg(env);
    memset(data, target, (size_t)length);
    data = enif_make_new_binary(env, (size_t)length, &previous);
    if (data == NULL) return enif_make_badarg(env);
    memset(data, 0, (size_t)length);

    other = enif_alloc_env();
    if (other == NULL) return enif_make_badarg(env);
    before_copy = enif_make_copy(other, map);
    fields[0] = mutablekey_snapshot(env, map, current, previous);
    memset(mutable_bytes, target, (size_t)length);
    fields[1] = mutablekey_snapshot(env, map, current, previous);
    copied_back = enif_make_copy(env, before_copy);
    fields[2] = mutablekey_snapshot(env, copied_back, current, previous);
    after_copy = enif_make_copy(other, map);
    copied_back = enif_make_copy(env, after_copy);
    fields[3] = mutablekey_snapshot(env, copied_back, current, previous);
    enif_clear_env(other);
    fields[4] = mutablekey_snapshot(env, copied_back, current, previous);
    enif_free_env(other);

    sentinel = mutablekey_number(env, -313);
    updated = sentinel;
    status = enif_make_map_update(env, map, current, mutablekey_number(env, 700), &updated);
    fields[5] = mutablekey_number(env, status);
    fields[6] = mutablekey_number(env, updated != sentinel);
    fields[7] = status ? mutablekey_snapshot(env, updated, current, previous) : mutablekey_number(env, -1);
    /* OTP's 33-to-32 conversion requires unique structural keys. A duplicate
       created after insertion violates that precondition and the original VM
       faults. Retain all other observations; never invoke this removal. */
    if (count == 33 && target == 1) {
        fields[8] = mutablekey_number(env, -2);
    } else {
        removed = sentinel;
        status = enif_make_map_remove(env, map, current, &removed);
        fields[8] = status ? mutablekey_snapshot(env, removed, current, previous) : mutablekey_number(env, -1);
    }
    inserted = sentinel;
    status = enif_make_map_put(env, map, current, mutablekey_number(env, 777), &inserted);
    fields[9] = status ? mutablekey_snapshot(env, inserted, current, previous) : mutablekey_number(env, -1);
    fields[10] = mutablekey_snapshot(env, map, current, previous);

    /* Duplicate flat keys cannot collapse merely because equality is invoked.
       The singleton is ordinary and safe; equality checks size before keys. */
    singleton = enif_make_new_map(env);
    if (!enif_make_map_put(env, singleton, current, mutablekey_number(env, 101), &singleton))
        return enif_make_badarg(env);
    fields[11] = mutablekey_number(env, enif_is_identical(map, singleton));
    return mutablekey_tuple(env, mutablekey_RESULT_FIELDS, fields);
}



static ERL_NIF_TERM replacement_key(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM keys[65], values[65], map, argument, changed, fields[6];
    unsigned char *data, *temporary;
    int count, length, mode, same, index, status;
    if (argc != 4 || !enif_get_int(env, argv[0], &count) || !enif_get_int(env, argv[1], &length) ||
        !enif_get_int(env, argv[2], &mode) || !enif_get_int(env, argv[3], &same) ||
        count < 2 || count > 65 || (length != 64 && length != 65) ||
        (mode != 0 && mode != 1) || (same != 0 && same != 1)) return enif_make_badarg(env);
    for (index = 0; index < count; index++) {
        data = enif_make_new_binary(env, (size_t)length, &keys[index]);
        if (data == NULL) return enif_make_badarg(env);
        memset(data, index, (size_t)length);
        values[index] = enif_make_int(env, 100 + index);
    }
    if (!enif_make_map_from_arrays(env, keys, values, (size_t)count, &map)) return enif_make_badarg(env);
    temporary = enif_make_new_binary(env, (size_t)length, &argument);
    if (temporary == NULL) return enif_make_badarg(env);
    memset(temporary, 0, (size_t)length);
    changed = enif_make_int(env, -313);
    if (mode == 0) status = enif_make_map_put(env, map, argument, enif_make_int(env, same ? 100 : 700), &changed);
    else status = enif_make_map_update(env, map, argument, enif_make_int(env, same ? 100 : 700), &changed);
    fields[0] = enif_make_int(env, status);
    fields[1] = enif_make_int(env, changed == map);
    if (!status) return enif_make_badarg(env);
    fields[2] = mutablekey_snapshot(env, map, argument, keys[0]);
    fields[3] = mutablekey_snapshot(env, changed, argument, keys[0]);
    memset(temporary, 128, (size_t)length);
    fields[4] = mutablekey_snapshot(env, map, argument, keys[0]);
    fields[5] = mutablekey_snapshot(env, changed, argument, keys[0]);
    return tuple(env, 6, fields);
}

static ErlNifFunc functions[] = {
    {"replacement_key", 4, replacement_key, 0},
    {"mutable_key", 3, mutablekey_run_case, 0},
    {"shape", 2, shape, 0},
    {"mutate", 4, mutate, 0},
    {"nonmap_null", 1, nonmap_null, 0},
    {"from_arrays", 1, from_arrays, 0},
    {"iterator", 2, iterator, 0},
    {"pending_value", 2, pending_value, 0},
    {"copy_boundary", 1, copy_boundary, 0},
    {"pending_key", 0, pending_key, 0},
    {"independent", 1, independent, 0},
    {"alias_output", 3, alias_output, 0},
    {"identity", 3, identity, 0}
};
ERL_NIF_INIT(map_family504, functions, NULL, NULL, NULL, NULL)
