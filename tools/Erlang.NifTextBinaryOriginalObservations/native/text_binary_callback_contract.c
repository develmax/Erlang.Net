#include "erl_nif.h"
#include "entry_fixture_names.h"
#include <string.h>
#include <stddef.h>

_Static_assert(sizeof(ErlNifBinary) == 40, "x64 binary descriptor");
_Static_assert(offsetof(ErlNifBinary, size) == 0, "size offset");
_Static_assert(offsetof(ErlNifBinary, data) == 8, "data offset");

static ErlNifBinary persistent;
static int has_persistent;

static ERL_NIF_TERM integer(ErlNifEnv *env, int value)
{
    return enif_make_int(env, value);
}

static ERL_NIF_TERM bytes(ErlNifEnv *env, const unsigned char *source, size_t size)
{
    ERL_NIF_TERM term;
    unsigned char *target = enif_make_new_binary(env, size, &term);
    if (size != 0) memcpy(target, source, size);
    return term;
}

static ERL_NIF_TERM tuple(ErlNifEnv *env, ERL_NIF_TERM *items, unsigned count)
{
    return enif_make_tuple_from_array(env, items, count);
}

static ERL_NIF_TERM observe(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int selector, status = 0;
    ERL_NIF_TERM out[5];
    unsigned char buffer[TB_BUFFER];
    unsigned length = 777;
    int arity = 777;
    const ERL_NIF_TERM *elements = NULL;
    memset(buffer, TB_CANARY, sizeof(buffer));
    if (argc != 2 || !enif_get_int(env, argv[0], &selector)) return enif_make_badarg(env);
    switch (selector) {
    case 0:
        status = enif_get_tuple(env, argv[1], &arity, &elements);
        out[0] = integer(env, status); out[1] = integer(env, arity);
        out[2] = status ? enif_make_list_from_array(env, elements, (unsigned)arity) : integer(env, 0);
        return tuple(env, out, 3);
    case 1:
        status = enif_get_list_length(env, argv[1], &length);
        out[0] = integer(env, status); out[1] = enif_make_uint(env, length);
        return tuple(env, out, 2);
    case 2:
    case 3:
        status = enif_get_atom(env, argv[1], (char *)buffer, TB_BUFFER, selector == 2 ? ERL_NIF_LATIN1 : ERL_NIF_UTF8);
        break;
    case 4:
    case 5:
        status = enif_get_atom_length(env, argv[1], &length, selector == 4 ? ERL_NIF_LATIN1 : ERL_NIF_UTF8);
        out[0] = integer(env, status); out[1] = enif_make_uint(env, length);
        return tuple(env, out, 2);
    case 6:
    case 7:
        status = enif_get_string(env, argv[1], (char *)buffer, 5, selector == 6 ? ERL_NIF_LATIN1 : ERL_NIF_UTF8);
        break;
    case 8:
        out[0] = enif_make_atom(env, "tb_static_atom");
        out[1] = enif_make_atom_len(env, "tb\0atom", 7);
        return tuple(env, out, 2);
    case 9:
    {
        ERL_NIF_TERM found = integer(env, 777);
        status = enif_make_existing_atom(env, "tb_missing_observer_7719284", &found, ERL_NIF_LATIN1);
        out[0] = integer(env, status); out[1] = found;
        out[2] = enif_make_atom(env, "tb_existing_atom");
        status = enif_make_existing_atom_len(env, "tb_existing_atom", 16, &found, ERL_NIF_LATIN1);
        out[3] = integer(env, status); out[4] = found;
        return tuple(env, out, 5);
    }
    case 10:
    {
        static const unsigned char utf8[] = {65,0,0xc3,0xa9,0xf0,0x9f,0x99,0x82};
        out[0] = enif_make_string_len(env, (const char *)utf8, sizeof(utf8), ERL_NIF_UTF8);
        out[1] = enif_make_string_len(env, (const char *)utf8, sizeof(utf8), ERL_NIF_LATIN1);
        return tuple(env, out, 2);
    }
    case 11:
    {
        unsigned char *parent;
        ERL_NIF_TERM original, slices[5], nested[2], container, children;
        parent = enif_make_new_binary(env, TB_PARENT, &original);
        memset(parent, 65, TB_PARENT);
        slices[0] = original;
        slices[1] = enif_make_sub_binary(env, original, 0, 63);
        slices[2] = enif_make_sub_binary(env, original, 0, 64);
        slices[3] = enif_make_sub_binary(env, original, 0, 65);
        slices[4] = enif_make_sub_binary(env, slices[3], 0, 64);
        container = tuple(env, slices, 5);
        children = enif_make_list_from_array(env, slices, 5);
        nested[0] = container; nested[1] = children;
        container = tuple(env, nested, 2);
        /* Every retained graph/borrowed tuple handle predates this valid new_binary write. */
        if (!enif_get_tuple(env, container, &arity, &elements)) return enif_make_badarg(env);
        memset(parent, 66, TB_PARENT);
        return enif_make_list_from_array(env, elements, (unsigned)arity);
    }
    case 12:
    {
        ErlNifBinary descriptor;
        ERL_NIF_TERM original;
        int allocated, resized;
        memset(&descriptor, 0, sizeof(descriptor));
        allocated = enif_alloc_binary(7, &descriptor);
        if (!allocated) return integer(env, allocated);
        memset(descriptor.data, 67, descriptor.size);
        resized = enif_realloc_binary(&descriptor, 9);
        if (!resized) { enif_release_binary(&descriptor); return integer(env, resized); }
        descriptor.data[7] = 68; descriptor.data[8] = 69;
        original = enif_make_binary(env, &descriptor);
        out[0] = integer(env, allocated); out[1] = integer(env, resized); out[2] = original;
        return tuple(env, out, 3);
    }
    case 13:
    {
        ErlNifBinary descriptor;
        int inspected, resized;
        memset(&descriptor, 0, sizeof(descriptor));
        inspected = enif_inspect_binary(env, argv[1], &descriptor);
        if (!inspected) return integer(env, inspected);
        resized = enif_realloc_binary(&descriptor, descriptor.size + 1);
        if (!resized) return integer(env, resized);
        descriptor.data[descriptor.size - 1] = 70;
        out[0] = argv[1]; out[1] = integer(env, inspected); out[2] = integer(env, resized);
        out[3] = enif_make_binary(env, &descriptor);
        return tuple(env, out, 4);
    }
    case 14:
    {
        ErlNifBinary descriptor;
        memset(&descriptor, 0, sizeof(descriptor));
        status = enif_inspect_iolist_as_binary(env, argv[1], &descriptor);
        out[0] = integer(env, status);
        out[1] = status ? bytes(env, descriptor.data, descriptor.size) : integer(env, 0);
        return tuple(env, out, 2);
    }
    case 15:
    {
        ErlNifBinary descriptor;
        memset(&descriptor, 0, sizeof(descriptor));
        status = enif_alloc_binary(3, &descriptor);
        if (!status) return integer(env, status);
        out[0] = integer(env, status);
        out[1] = enif_make_uint(env, (unsigned)descriptor.size);
        enif_release_binary(&descriptor);
        /* Void release is invoked; no descriptor field or dangling pointer is read afterwards. */
        return tuple(env, out, 2);
    }
    case 16:
    {
        unsigned char *data;
        ERL_NIF_TERM binary, reason, raised;
        data = enif_make_new_binary(env, 65, &binary);
        memset(data, 65, 65);
        out[0] = binary; reason = tuple(env, out, 1);
        raised = enif_raise_exception(env, reason);
        memset(data, 66, 65);
        return raised;
    }
    case 17:
    {
        unsigned char *data;
        ERL_NIF_TERM binary, iolist;
        ErlNifBinary inspected;
        data = enif_make_new_binary(env, 65, &binary);
        memset(data, 65, 65);
        out[0] = integer(env, 1); out[1] = binary;
        iolist = enif_make_list_from_array(env, out, 2);
        memset(&inspected, 0, sizeof(inspected));
        status = enif_inspect_iolist_as_binary(env, iolist, &inspected);
        if (!status) return integer(env, status);
        out[0] = binary; out[1] = bytes(env, inspected.data, inspected.size);
        memset(data, 66, 65);
        return tuple(env, out, 2);
    }
    case 18:
        if (has_persistent) return enif_make_badarg(env);
        memset(&persistent, 0, sizeof(persistent));
        has_persistent = enif_alloc_binary(5, &persistent);
        if (has_persistent) memset(persistent.data, 71, persistent.size);
        return integer(env, has_persistent);
    case 19:
        if (!has_persistent) return enif_make_badarg(env);
        out[0] = enif_make_binary(env, &persistent);
        has_persistent = 0;
        return out[0];
    default: return enif_make_badarg(env);
    }
    out[0] = integer(env, status); out[1] = bytes(env, buffer, sizeof(buffer));
    return tuple(env, out, 2);
}

static ERL_NIF_TERM legacy(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int selector;
    char long_text[TB_LONG_BYTES + 1];
    static const unsigned char utf8[] = {0xc3,0xa9,0xf0,0x9f,0x99,0x82,0};
    static const unsigned char invalid[] = {0xff,0};
    unsigned i;
    if (argc != 1 || !enif_get_int(env, argv[0], &selector)) return enif_make_badarg(env);
    if (selector == 0) return enif_make_string(env, (const char *)utf8, ERL_NIF_UTF8);
    if (selector == 1) return enif_make_string(env, (const char *)invalid, ERL_NIF_UTF8);
    for (i = 0; i < TB_LONG_BYTES; ++i) long_text[i] = 'x';
    long_text[TB_LONG_BYTES] = 0;
    return enif_make_string(env, long_text, selector == 2 ? ERL_NIF_LATIN1 : ERL_NIF_UTF8);
}

static ERL_NIF_TERM widths(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM values[4];
    (void)argc; (void)argv;
    values[0] = integer(env, (int)sizeof(int)); values[1] = integer(env, (int)sizeof(long));
    values[2] = integer(env, (int)sizeof(size_t)); values[3] = integer(env, (int)sizeof(ErlNifBinary));
    return enif_make_list_from_array(env, values, 4);
}

static void unload(ErlNifEnv *env, void *private_data)
{
    (void)env; (void)private_data;
    if (has_persistent) { enif_release_binary(&persistent); has_persistent = 0; }
}

static ErlNifFunc functions[] = {
    {"observe",2,observe,0}, {"legacy",1,legacy,0}, {"widths",0,widths,0}
};
ERL_NIF_INIT(text_binary_callback_contract, functions, NULL, NULL, NULL, unload)
