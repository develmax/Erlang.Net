#include "erl_nif.h"
#include <stdio.h>
#if defined(_WIN32)
#include <windows.h>
#else
#include <pthread.h>
#include <time.h>
#endif

/* One physical library for BOTH generations. Never build A/B copies. */
enum { Accept709 = 0, Veto709 = 1 };
typedef struct { int generation; unsigned allocations; unsigned frees; } State709;
static char log709[4096];
static ErlNifEnv* (*first_allocator709)(void);

typedef struct { ErlNifEnv* env; ERL_NIF_TERM term; } Thread709;

#if defined(_WIN32)
static DWORD WINAPI foreign709(LPVOID argument)
#else
static void* foreign709(void* argument)
#endif
{
    Thread709* state = (Thread709*)argument;
    state->env = enif_alloc_env();
    if (state->env) state->term = enif_make_int(state->env, 709);
    return 0;
}

static int event709(const char* event, int generation)
{
    FILE* output = fopen(log709, "ab");
    if (!output) return 0;
    int written = fprintf(output, "%s %d\n", event, generation) > 0;
    return fclose(output) == 0 && written;
}

static int info709(ErlNifEnv* env, ERL_NIF_TERM info, int* mode)
{
    int arity;
    const ERL_NIF_TERM* fields;
    return enif_get_tuple(env, info, &arity, &fields) && arity == 2
        && enif_get_string(env, fields[0], log709, sizeof(log709), ERL_NIF_LATIN1) > 0
        && enif_get_int(env, fields[1], mode)
        && (*mode == Accept709 || *mode == Veto709);
}

static int load709(ErlNifEnv* env, void** private_data, ERL_NIF_TERM info)
{
    int mode;
    if (!info709(env, info, &mode)) return 41;
    State709* state = enif_alloc(sizeof(*state));
    if (!state) return 42;
    state->generation = 1;
    first_allocator709 = enif_alloc_env;
    state->allocations = state->frees = 0;
    if (!event709("load", 1)) { enif_free(state); return 43; }
    *private_data = state;
    return 0;
}

static int upgrade709(ErlNifEnv* env, void** next_data, void** old_data,
    ERL_NIF_TERM info)
{
    int mode;
    if (!info709(env, info, &mode)) return 51;
    State709* old = *old_data;
    if (!old || !event709("upgrade", old->generation)) return 52;
    if (mode == Veto709) {
        /* Old memory remains live. OTP restores the pointer CELL after veto. */
        *old_data = NULL;
        *next_data = NULL;
        (void)event709("veto", old->generation);
        return 7;
    }
    State709* next = enif_alloc(sizeof(*next));
    if (!next) return 53;
    next->generation = old->generation + 1;
    next->allocations = next->frees = 0;
    if (!event709("accept", next->generation)) { enif_free(next); return 54; }
    *next_data = next;
    /* Leave *old_data alive for genuine old-code invocations and unload. */
    return 0;
}

static void unload709(ErlNifEnv* env, void* private_data)
{
    (void)env;
    State709* state = private_data;
    (void)event709("unload", state ? state->generation : 0);
    if (state) enif_free(state);
}

static ERL_NIF_TERM roundtrip709(ErlNifEnv* caller, int argc,
    const ERL_NIF_TERM argv[])
{
    if (argc != 1) return enif_make_badarg(caller);
    State709* state = enif_priv_data(caller);
    if (!state) return enif_make_badarg(caller);
    ErlNifEnv* independent = enif_alloc_env();
    if (!independent) return enif_make_atom(caller, "allocation_failed");
    state->allocations++;
    ERL_NIF_TERM copied = enif_make_copy(independent, argv[0]);
    ERL_NIF_TERM tuple = enif_make_tuple2(independent,
        enif_make_int(independent, state->generation), copied);
    ERL_NIF_TERM result = enif_make_copy(caller, tuple);
    enif_free_env(independent);
    state->frees++;
    return result;
}

static ERL_NIF_TERM counts709(ErlNifEnv* caller, int argc,
    const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    State709* state = enif_priv_data(caller);
    if (!state) return enif_make_badarg(caller);
    return enif_make_tuple3(caller, enif_make_int(caller, state->generation),
        enif_make_uint(caller, state->allocations), enif_make_uint(caller, state->frees));
}

static ERL_NIF_TERM stable709(ErlNifEnv* caller, int argc,
    const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_atom(caller,
        first_allocator709 == enif_alloc_env ? "true" : "false");
}

static ERL_NIF_TERM thread709(ErlNifEnv* caller, int argc,
    const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    Thread709* state = enif_alloc(sizeof(*state));
    if (!state) return enif_make_badarg(caller);
    state->env = NULL;
    state->term = 0;
#if defined(_WIN32)
    HANDLE thread = CreateThread(NULL, 0, foreign709, state, 0, NULL);
    if (!thread) { enif_free(state); return enif_make_badarg(caller); }
    DWORD wait = WaitForSingleObject(thread, 10000);
    CloseHandle(thread);
    if (wait != WAIT_OBJECT_0) {
        /* Worker may still touch state: retain it, fail the owned run, never
         * claim successful cleanup or free another thread's live storage. */
        return enif_make_atom(caller, "thread_join_failed");
    }
#else
    pthread_t thread;
    if (pthread_create(&thread, NULL, foreign709, state) != 0) {
        enif_free(state); return enif_make_badarg(caller);
    }
    struct timespec deadline;
    if (clock_gettime(CLOCK_REALTIME, &deadline) != 0)
        return enif_make_atom(caller, "thread_join_failed");
    deadline.tv_sec += 10;
    if (pthread_timedjoin_np(thread, NULL, &deadline) != 0)
        return enif_make_atom(caller, "thread_join_failed");
#endif
    if (!state->env) { enif_free(state); return enif_make_badarg(caller); }
    ERL_NIF_TERM result = enif_make_copy(caller, state->term);
    enif_free_env(state->env);
    enif_free(state);
    return result;
}

static ErlNifFunc functions709[] = {
    {"roundtrip", 1, roundtrip709, 0},
    {"counts", 0, counts709, 0},
    {"allocator_stable", 0, stable709, 0},
    {"foreign_thread", 0, thread709, 0}
};
ERL_NIF_INIT(nif_env709, functions709, load709, NULL, upgrade709, unload709)
