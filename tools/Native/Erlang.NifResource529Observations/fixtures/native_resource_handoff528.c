#include "erl_nif.h"
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
static volatile LONG destroyed528, observed528 = -1;
static void finish528(int value) { InterlockedExchange(&observed528, value); InterlockedIncrement(&destroyed528); }
static long count528(void) { return InterlockedCompareExchange(&destroyed528, 0, 0); }
static long marker528(void) { return InterlockedCompareExchange(&observed528, 0, 0); }
#else
#include <fcntl.h>
#include <unistd.h>
static long destroyed528, observed528 = -1;
static void finish528(int value) { __atomic_store_n(&observed528, value, __ATOMIC_SEQ_CST); __atomic_fetch_add(&destroyed528, 1, __ATOMIC_SEQ_CST); }
static long count528(void) { return __atomic_load_n(&destroyed528, __ATOMIC_SEQ_CST); }
static long marker528(void) { return __atomic_load_n(&observed528, __ATOMIC_SEQ_CST); }
#endif
#ifndef RESOURCE_VERSION528
#define RESOURCE_VERSION528 1
#endif
#define CHILD_BYTES528 64
#define MARKER528 0x5a
#define PATH_BYTES528 4096
typedef struct { unsigned char *child; size_t length; } resource528;
typedef struct { char unload_path[PATH_BYTES528]; } state528;
static ErlNifResourceType *type528;
static int tried528, mode528;
static char destructor_path528[PATH_BYTES528 + 7];
static char final_path528[PATH_BYTES528 + 7];
static void finalized528(void)
{
    if (final_path528[0] == 0) return;
    char version = (char)('0' + RESOURCE_VERSION528);
#ifdef _WIN32
    HANDLE file = CreateFileA(final_path528, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (file != INVALID_HANDLE_VALUE) { DWORD written; WriteFile(file, &version, 1, &written, NULL); CloseHandle(file); }
#else
    int file = open(final_path528, O_WRONLY | O_CREAT | O_TRUNC, 0600);
    if (file >= 0) { (void)write(file, &version, 1); (void)close(file); }
#endif
}
#ifdef _WIN32
BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID reserved)
{
    (void)instance; (void)reserved;
    if (reason == DLL_PROCESS_DETACH) finalized528();
    return TRUE;
}
#else
__attribute__((destructor)) static void image_finalized528(void) { finalized528(); }
#endif
static void destroy528(ErlNifEnv *env, void *value)
{
    (void)env;
    resource528 *resource = value;
    unsigned char saved[CHILD_BYTES528];
    size_t captured = resource->length == CHILD_BYTES528 ? CHILD_BYTES528 : 0;
    if (captured) memcpy(saved, resource->child, captured);
    int marker = captured ? saved[0] : -2;
    enif_free(resource->child);
    finish528(marker);
    FILE *file = fopen(destructor_path528, "wb");
    if (file != NULL) {
        fputc('0' + RESOURCE_VERSION528, file);
        if (captured) (void)fwrite(saved, 1, captured, file);
        fclose(file);
    }
}
static int initialize528(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info, int takeover)
{
    state528 temporary = {{0}};
    int result = enif_get_string(env, info, temporary.unload_path, PATH_BYTES528, ERL_NIF_LATIN1);
    if (result <= 0) return -1;
    memcpy(final_path528, temporary.unload_path, (size_t)result - 1);
    memcpy(final_path528 + result - 1, ".final", sizeof(".final"));
    memcpy(destructor_path528, temporary.unload_path, (size_t)result - 1);
    memcpy(destructor_path528 + result - 1, ".dtor", sizeof(".dtor"));
    mode528 = takeover;
    ErlNifResourceFlags tried;
    type528 = enif_open_resource_type(env, NULL, "resource528", destroy528,
        takeover ? ERL_NIF_RT_TAKEOVER : (ERL_NIF_RT_CREATE | ERL_NIF_RT_TAKEOVER), &tried);
    tried528 = (int)tried;
    if (type528 == NULL) return -1;
    state528 *state = enif_alloc(sizeof(state528));
    if (state == NULL) return -1;
    memcpy(state, &temporary, sizeof(state528));
    *private_data = state;
    return 0;
}
static int load528(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    return initialize528(env, private_data, info, 0);
}
static int upgrade528(ErlNifEnv *env, void **private_data, void **old_private, ERL_NIF_TERM info)
{
    (void)old_private;
    return initialize528(env, private_data, info, 1);
}
static void unload528(ErlNifEnv *env, void *private_data)
{
    (void)env;
    state528 *state = private_data;
    char path[PATH_BYTES528];
    memcpy(path, state->unload_path, sizeof(path));
    enif_free(state);
    FILE *file = fopen(path, "wb");
    if (file != NULL) { fprintf(file, "%d\n", RESOURCE_VERSION528); fclose(file); }
}
static ERL_NIF_TERM make528(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    resource528 *resource = enif_alloc_resource(type528, sizeof(resource528));
    if (resource == NULL) return enif_make_badarg(env);
    resource->length = CHILD_BYTES528;
    resource->child = enif_alloc(CHILD_BYTES528);
    if (resource->child == NULL) { resource->length = 0; enif_release_resource(resource); return enif_make_badarg(env); }
    memset(resource->child, MARKER528, CHILD_BYTES528);
    ERL_NIF_TERM result = enif_make_resource(env, resource);
    enif_release_resource(resource);
    return result;
}
static ERL_NIF_TERM inspect528(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    resource528 *resource;
    if (argc != 1 || !enif_get_resource(env, argv[0], type528, (void **)&resource)) return enif_make_badarg(env);
    int all = resource->length == CHILD_BYTES528;
    for (size_t i = 0; i < resource->length; i++) all &= resource->child[i] == MARKER528;
    return enif_make_tuple4(env, enif_make_int(env, RESOURCE_VERSION528), enif_make_int(env, all),
        enif_make_uint64(env, (ErlNifUInt64)resource->length), enif_make_uint64(env, (ErlNifUInt64)enif_sizeof_resource(resource)));
}
static ERL_NIF_TERM counts528(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_tuple3(env, enif_make_int(env, RESOURCE_VERSION528),
        enif_make_long(env, count528()), enif_make_long(env, marker528()));
}
static ERL_NIF_TERM probe528(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    resource528 *resource = (resource528 *)(uintptr_t)1;
    if (argc != 1) return enif_make_badarg(env);
    int matched = enif_get_resource(env, argv[0], type528, (void **)&resource);
    int untouched = resource == (resource528 *)(uintptr_t)1;
    ERL_NIF_TERM bytes = enif_make_list(env, 0);
    if (matched) {
        for (size_t i = resource->length; i > 0; i--)
            bytes = enif_make_list_cell(env, enif_make_uint(env, resource->child[i-1]), bytes);
    }
    return enif_make_tuple6(env, enif_make_int(env, RESOURCE_VERSION528),
        enif_make_int(env, mode528), enif_make_int(env, tried528),
        enif_make_int(env, matched), enif_make_int(env, untouched), bytes);
}
static ErlNifFunc functions528[] = {
    {"probe528", 1, probe528, 0}, {"make528", 0, make528, 0}, {"inspect528", 1, inspect528, 0}, {"counts528", 0, counts528, 0}
};
ERL_NIF_INIT(native_resource_handoff528, functions528, load528, NULL, upgrade528, unload528)
