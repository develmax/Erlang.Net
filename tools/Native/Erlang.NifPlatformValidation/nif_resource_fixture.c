#include "erl_nif.h"
#include <stdint.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
static volatile LONG destroyed;
static void record_destroy(void) { InterlockedIncrement(&destroyed); }
static int count(void) { return (int)InterlockedCompareExchange(&destroyed, 0, 0); }
#else
static int destroyed;
static void record_destroy(void) { __atomic_add_fetch(&destroyed, 1, __ATOMIC_SEQ_CST); }
static int count(void) { return __atomic_load_n(&destroyed, __ATOMIC_SEQ_CST); }
#endif
static ErlNifResourceType *resource_type;
static void *held;
static void destroy(ErlNifEnv *env, void *object) { (void)env; (void)object; record_destroy(); }
static int load(ErlNifEnv *env, void **priv, ERL_NIF_TERM info) {
    ErlNifResourceFlags tried;
    (void)priv; (void)info;
    resource_type = enif_open_resource_type(env, NULL, "word_resource", destroy, ERL_NIF_RT_CREATE, &tried);
    return resource_type == NULL ? -1 : 0;
}
static ERL_NIF_TERM create(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    uint64_t value = UINT64_C(0xfedcba9876543210);
    unsigned char *data = enif_alloc_resource(resource_type, 16);
    ERL_NIF_TERM ref, bytes;
    (void)argc; (void)argv;
    if (data == NULL) return enif_make_badarg(env);
    if (((uintptr_t)data & 7) != 0) { enif_release_resource(data); return enif_make_badarg(env); }
    memcpy(data, &value, 8);
    memcpy(data + 8, "resource", 8);
    ref = enif_make_resource(env, data);
    bytes = enif_make_resource_binary(env, data, data + 8, 8);
    enif_release_resource(data);
    return enif_make_tuple3(env, ref, bytes, enif_make_uint64(env, value));
}
static ERL_NIF_TERM get(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    void *data; uint64_t value;
    (void)argc;
    if (!enif_get_resource(env, argv[0], resource_type, &data)) return enif_make_badarg(env);
    memcpy(&value, data, 8);
    return enif_make_tuple2(env, enif_make_uint64(env, value), enif_make_uint64(env, enif_sizeof_resource(data)));
}
static ERL_NIF_TERM keep(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    void *data; (void)argc;
    if (held != NULL || !enif_get_resource(env, argv[0], resource_type, &data)) return enif_make_badarg(env);
    enif_keep_resource(data); held = data;
    return enif_make_atom(env, "ok");
}
static ERL_NIF_TERM release(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    void *data = held; (void)argc; (void)argv;
    if (data == NULL) return enif_make_badarg(env);
    held = NULL; enif_release_resource(data);
    return enif_make_atom(env, "ok");
}
static ERL_NIF_TERM stats(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    (void)argc; (void)argv;
    return enif_make_tuple2(env, enif_make_int(env, count()), enif_make_int(env, held != NULL));
}
static ERL_NIF_TERM native_only(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    void *data = enif_alloc_resource(resource_type, 16); (void)argc; (void)argv;
    if (data == NULL) return enif_make_badarg(env);
    enif_release_resource(data);
    return enif_make_atom(env, "ok");
}
static ErlNifFunc functions[] = {
    {"new", 0, create, 0}, {"inspect", 1, get, 0}, {"keep", 1, keep, 0},
    {"release", 0, release, 0}, {"stats", 0, stats, 0}, {"native_only", 0, native_only, 0}
};
ERL_NIF_INIT(nif_resource_fixture, functions, load, NULL, NULL, NULL)
