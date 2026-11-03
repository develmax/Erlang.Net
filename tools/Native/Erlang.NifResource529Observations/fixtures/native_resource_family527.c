#include "erl_nif.h"
#include <stddef.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
static volatile LONG destroyed527;
static volatile LONG destructor_thread527 = 99;
static void record_thread527(void) { InterlockedExchange(&destructor_thread527, enif_thread_type()); }
static int thread527(void) { return (int)InterlockedCompareExchange(&destructor_thread527, 0, 0); }
static void retire527(void) { InterlockedIncrement(&destroyed527); }
static long count527(void) { return InterlockedCompareExchange(&destroyed527, 0, 0); }
#else
static long destroyed527;
static int destructor_thread527 = 99;
static void record_thread527(void) { __atomic_store_n(&destructor_thread527, enif_thread_type(), __ATOMIC_SEQ_CST); }
static int thread527(void) { return __atomic_load_n(&destructor_thread527, __ATOMIC_SEQ_CST); }
static void retire527(void) { __atomic_fetch_add(&destroyed527, 1, __ATOMIC_SEQ_CST); }
static long count527(void) { return __atomic_load_n(&destroyed527, __ATOMIC_SEQ_CST); }
#endif
#ifndef RESOURCE_VERSION527
#define RESOURCE_VERSION527 1
#endif
#define PAYLOAD_BYTES527 4096
static ErlNifResourceType *legacy527, *extended527, *dynamic527;
static int opened527[3];
static int independent_open527, independent_tried527;
static void destroy527(ErlNifEnv *env, void *resource)
{
    (void)env; (void)resource;
    record_thread527();
    retire527();
}
static void dynamic_call527(ErlNifEnv *env, void *resource, void *data)
{
    (void)env;
    unsigned char *bytes = resource;
    int *out = data;
    bytes[0] = (unsigned char)RESOURCE_VERSION527;
    *out = RESOURCE_VERSION527;
}
static int open527(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info, int takeover)
{
    int fail = 0;
    ErlNifResourceFlags tried;
    ErlNifResourceFlags flags = takeover ? ERL_NIF_RT_TAKEOVER : ERL_NIF_RT_CREATE;
    ErlNifResourceTypeInit init;
    (void)private_data;
    (void)enif_get_int(env, info, &fail);
    memset(&init, 0, sizeof(init));
    init.dtor = destroy527;
    legacy527 = enif_open_resource_type(env, NULL, "legacy527", destroy527, flags, &tried);
    opened527[0] = (int)tried;
    extended527 = enif_open_resource_type_x(env, "extended527", &init, flags, &tried);
    opened527[1] = (int)tried;
    init.members = 4;
    init.dyncall = dynamic_call527;
    dynamic527 = enif_init_resource_type(env, "dynamic527", &init, flags, &tried);
    opened527[2] = (int)tried;
    ErlNifEnv *independent = enif_alloc_env();
    ErlNifResourceType *outside = enif_open_resource_type(independent, NULL, "independent527", destroy527,
        ERL_NIF_RT_CREATE, &tried);
    independent_open527 = outside != NULL;
    independent_tried527 = (int)tried;
    enif_free_env(independent);
    if (fail) {
        (void)enif_open_resource_type(env, NULL, "rollback527", destroy527, ERL_NIF_RT_CREATE, &tried);
        return -1;
    }
    return legacy527 == NULL || extended527 == NULL || dynamic527 == NULL ? -1 : 0;
}
static int load527(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    return open527(env, private_data, info, 0);
}
static int upgrade527(ErlNifEnv *env, void **private_data, void **old_private, ERL_NIF_TERM info)
{
    (void)old_private;
    return open527(env, private_data, info, 1);
}
static ERL_NIF_TERM opens527(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    ERL_NIF_TERM rows[] = {enif_make_int(env, RESOURCE_VERSION527),
        enif_make_int(env, opened527[0]), enif_make_int(env, opened527[1]), enif_make_int(env, opened527[2]),
        enif_make_int(env, independent_open527), enif_make_int(env, independent_tried527)};
    return enif_make_tuple_from_array(env, rows, (unsigned)(sizeof(rows) / sizeof(rows[0])));
}
static ERL_NIF_TERM outside527(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    ERL_NIF_TERM rows[5];
    for (int flags = 0; flags < 5; flags++) {
        ErlNifResourceFlags tried = (ErlNifResourceFlags)99;
        ErlNifResourceType *type = enif_open_resource_type(env, NULL, "outside527", destroy527,
            (ErlNifResourceFlags)flags, &tried);
        rows[flags] = enif_make_tuple3(env, enif_make_int(env, flags),
            enif_make_int(env, type != NULL), enif_make_int(env, (int)tried));
    }
    return enif_make_list_from_array(env, rows, 5);
}
static ERL_NIF_TERM make527(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    unsigned char *resource = enif_alloc_resource(dynamic527, PAYLOAD_BYTES527);
    if (resource == NULL) return enif_make_badarg(env);
    for (unsigned i = 0; i < PAYLOAD_BYTES527; i++) resource[i] = (unsigned char)i;
    enif_keep_resource(resource);
    ERL_NIF_TERM reference = enif_make_resource(env, resource);
    ERL_NIF_TERM alias = enif_make_resource(env, resource);
    ERL_NIF_TERM binary = enif_make_resource_binary(env, resource, resource, PAYLOAD_BYTES527);
    ERL_NIF_TERM short_binary527 = enif_make_sub_binary(env, binary, 0, 63);
    ERL_NIF_TERM large = enif_make_sub_binary(env, binary, 0, 65);
    void *out = NULL;
    int get_ref = enif_get_resource(env, reference, dynamic527, &out) && out == resource;
    int get_binary = enif_get_resource(env, binary, dynamic527, &out) && out == resource;
    void *sentinel = (void *)(size_t)7;
    int wrong_type = enif_get_resource(env, reference, legacy527, &sentinel);
    int unchanged_wrong_type = sentinel == (void *)(size_t)7;
    int null_type = enif_get_resource(env, reference, NULL, &sentinel);
    int unchanged_null_type = sentinel == (void *)(size_t)7;
    int get_small = enif_get_resource(env, short_binary527, dynamic527, &out);
    int get_large = enif_get_resource(env, large, dynamic527, &out) && out == resource;
    int data = -1;
    int missing = enif_dynamic_resource_call(env, enif_make_atom(env, "native_resource_family527"),
        enif_make_atom(env, "missing527"), reference, &data);
    int dynamic = enif_dynamic_resource_call(env, enif_make_atom(env, "native_resource_family527"),
        enif_make_atom(env, "dynamic527"), reference, &data);
    ERL_NIF_TERM values[] = {
        enif_make_int(env, get_ref), enif_make_int(env, get_binary),
        enif_make_int(env, enif_is_identical(reference, alias)), enif_make_int(env, get_small),
        enif_make_int(env, get_large), enif_make_int(env, wrong_type), enif_make_int(env, unchanged_wrong_type),
        enif_make_int(env, null_type), enif_make_int(env, unchanged_null_type), enif_make_uint64(env, (ErlNifUInt64)enif_sizeof_resource(resource)),
        enif_make_int(env, missing), enif_make_int(env, dynamic), enif_make_int(env, data)
    };
    ERL_NIF_TERM result = enif_make_tuple3(env, reference, binary,
        enif_make_tuple_from_array(env, values, (unsigned)(sizeof(values) / sizeof(values[0]))));
    enif_release_resource(resource);
    enif_release_resource(resource);
    return result;
}
static ERL_NIF_TERM inspect527(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    void *resource;
    int data = -1;
    if (argc != 1 || !enif_get_resource(env, argv[0], dynamic527, &resource)) return enif_make_badarg(env);
    int result = enif_dynamic_resource_call(env, enif_make_atom(env, "native_resource_family527"),
        enif_make_atom(env, "dynamic527"), argv[0], &data);
    return enif_make_tuple3(env, enif_make_int(env, result), enif_make_int(env, data),
        enif_make_uint64(env, (ErlNifUInt64)enif_sizeof_resource(resource)));
}
static ERL_NIF_TERM count_nif527(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_tuple3(env, enif_make_int(env, RESOURCE_VERSION527), enif_make_long(env, count527()),
        enif_make_int(env, thread527()));
}
static ErlNifFunc functions527[] = {
    {"opens527", 0, opens527, 0}, {"outside527", 0, outside527, 0},
    {"make527", 0, make527, 0}, {"inspect527", 1, inspect527, 0}, {"count527", 0, count_nif527, 0}
};
ERL_NIF_INIT(native_resource_family527, functions527, load527, NULL, upgrade527, NULL)
