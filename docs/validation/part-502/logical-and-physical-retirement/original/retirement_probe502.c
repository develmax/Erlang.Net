#include "erl_nif.h"
#include <windows.h>

static HANDLE gate;
static volatile LONG entered;
static volatile LONG done;
static volatile LONG wait_result = -1;

static int load(ErlNifEnv *env, void **data, ERL_NIF_TERM info)
{
    (void)env; (void)data; (void)info;
    gate = CreateEventW(NULL, TRUE, FALSE, NULL);
    return gate == NULL ? -1 : 0;
}

static void unload(ErlNifEnv *env, void *data)
{
    (void)env; (void)data;
    if (gate != NULL) { SetEvent(gate); CloseHandle(gate); gate = NULL; }
}

static ERL_NIF_TERM hold(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    DWORD result;
    (void)argc; (void)argv;
    InterlockedExchange(&entered, 1);
    result = WaitForSingleObject(gate, 10000);
    InterlockedExchange(&wait_result, (LONG)result);
    InterlockedExchange(&done, 1);
    return enif_make_atom(env, result == WAIT_OBJECT_0 ? "released" : "bounded_timeout");
}

static ERL_NIF_TERM release(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_atom(env, SetEvent(gate) ? "ok" : "release_failed");
}

static ERL_NIF_TERM status(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM fields[3];
    (void)argc; (void)argv;
    fields[0] = enif_make_int(env, (int)InterlockedCompareExchange(&entered, 0, 0));
    fields[1] = enif_make_int(env, (int)InterlockedCompareExchange(&done, 0, 0));
    fields[2] = enif_make_int(env, (int)InterlockedCompareExchange(&wait_result, 0, 0));
    return enif_make_tuple_from_array(env, fields, 3);
}

static ErlNifFunc functions[] = {
    {"hold", 0, hold, ERL_NIF_DIRTY_JOB_CPU_BOUND},
    {"release", 0, release, 0},
    {"status", 0, status, 0}
};
ERL_NIF_INIT(retirement_probe502, functions, load, NULL, NULL, unload)
