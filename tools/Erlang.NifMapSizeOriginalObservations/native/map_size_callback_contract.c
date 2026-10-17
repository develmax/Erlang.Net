#include "erl_nif.h"
#include "entry_fixture_names.h"
#include <stddef.h>
#ifdef _WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif
static ErlNifEnv *retained_env;
static ERL_NIF_TERM retained_map;
static int worker_status;
static size_t worker_size;
#ifdef _WIN32
static DWORD WINAPI size_worker(LPVOID ignored)
#else
static void *size_worker(void *ignored)
#endif
{
    (void)ignored;
    worker_size = (size_t)-1;
    worker_status = enif_get_map_size(NULL, retained_map, &worker_size);
    return 0;
}
static int run_worker(void)
{
#ifdef _WIN32
    HANDLE thread = CreateThread(NULL,0,size_worker,NULL,0,NULL);
    DWORD wait;
    if (thread == NULL) return 0;
    wait = WaitForSingleObject(thread,INFINITE);
    CloseHandle(thread);
    return wait == WAIT_OBJECT_0;
#else
    pthread_t thread;
    if (pthread_create(&thread,NULL,size_worker,NULL)) return 0;
    return pthread_join(thread,NULL) == 0;
#endif
}
static void unload(ErlNifEnv *env, void *private_data)
{
    (void)env; (void)private_data;
    if (retained_env != NULL) { enif_free_env(retained_env); retained_env = NULL; }
}
static ERL_NIF_TERM observe(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int mode, status, refused = -1;
    size_t storage[3] = {17, MB_SENTINEL, 29};
    ERL_NIF_TERM values[5];
    if (argc != 2 || !enif_get_int(env, argv[0], &mode)) return enif_make_badarg(env);
    if (mode == 3) {
        if (retained_env != NULL) return enif_make_badarg(env);
        retained_env = enif_alloc_env();
        if (retained_env == NULL) return enif_make_badarg(env);
        retained_map = enif_make_copy(retained_env, argv[1]);
        status = enif_get_map_size(NULL, retained_map, &storage[1]);
    } else if (mode == 4) {
        if (retained_env == NULL || !run_worker()) return enif_make_badarg(env);
        status = worker_status; storage[1] = worker_size;
    } else if (mode == 5) {
        if (retained_env == NULL) return enif_make_badarg(env);
        status = enif_get_map_size(NULL, retained_map, &storage[1]);
        enif_free_env(retained_env); retained_env = NULL;
    } else status = enif_get_map_size(mode == 1 ? NULL : env, argv[1], &storage[1]);
    if (mode == 2 && !status) refused = enif_get_map_size(NULL, argv[1], NULL);
    values[0] = enif_make_int(env, status);
    values[1] = enif_make_uint64(env, (ErlNifUInt64)storage[1]);
    values[2] = enif_make_uint64(env, (ErlNifUInt64)storage[0]);
    values[3] = enif_make_uint64(env, (ErlNifUInt64)storage[2]);
    values[4] = enif_make_int(env, refused);
    return enif_make_tuple_from_array(env, values, 5);
}
static ERL_NIF_TERM widths(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM values[4];
    (void)argc; (void)argv;
    values[0] = enif_make_int(env, (int)sizeof(int));
    values[1] = enif_make_int(env, (int)sizeof(long));
    values[2] = enif_make_int(env, (int)sizeof(void *));
    values[3] = enif_make_int(env, (int)sizeof(ErlNifBinary));
    return enif_make_list_from_array(env, values, 4);
}
static ErlNifFunc functions[] = {{"observe",2,observe,0},{"widths",0,widths,0}};
ERL_NIF_INIT(map_size_callback_contract,functions,NULL,NULL,NULL,unload)
