#define _POSIX_C_SOURCE 200809L
#include "erl_nif.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#ifndef _WIN32
#include <pthread.h>
#endif
#ifdef _WIN32
#include <windows.h>
typedef volatile LONG flag521;
static int read521(flag521 *p) { return (int)InterlockedCompareExchange(p, 0, 0); }
static void write521(flag521 *p, int value) { InterlockedExchange(p, value); }
static void init521(flag521 *p, int value) { InterlockedExchange(p, value); }
static void increment521(flag521 *p) { InterlockedIncrement(p); }
static void decrement521(flag521 *p) { InterlockedDecrement(p); }
static void pause521(void) { Sleep(1); }
#else
#include <stdatomic.h>
#include <time.h>
typedef atomic_int flag521;
static int read521(flag521 *p) { return atomic_load_explicit(p, memory_order_acquire); }
static void write521(flag521 *p, int value) { atomic_store_explicit(p, value, memory_order_release); }
static void init521(flag521 *p, int value) { atomic_init(p, value); }
static void increment521(flag521 *p) { atomic_fetch_add_explicit(p, 1, memory_order_acq_rel); }
static void decrement521(flag521 *p) { atomic_fetch_sub_explicit(p, 1, memory_order_acq_rel); }
static void pause521(void) { const struct timespec d = {0, 1000000}; nanosleep(&d, 0); }
#endif
#ifndef CPU_FLAGS521
#define CPU_FLAGS521 ERL_NIF_DIRTY_JOB_CPU_BOUND
#endif
enum { CONTROL_PATH_CAP521 = 4096 };
typedef struct {
    char barrier[CONTROL_PATH_CAP521];
    char marker[CONTROL_PATH_CAP521];
    char entered[CONTROL_PATH_CAP521];
    char done[CONTROL_PATH_CAP521];
    flag521 active, finished;
} control521;

static flag521 busy521, done521, released521, cpu_calls521, io_calls521;
static ERL_NIF_TERM atom521(ErlNifEnv *env, const char *s) { return enif_make_atom(env, s); }
static ERL_NIF_TERM observe521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int type = enif_thread_type();
    if (argc != 1) return enif_make_badarg(env);
    if (type == ERL_NIF_THR_DIRTY_CPU_SCHEDULER) increment521(&cpu_calls521);
    if (type == ERL_NIF_THR_DIRTY_IO_SCHEDULER) increment521(&io_calls521);
    return enif_make_tuple2(env, enif_make_int(env, type), argv[0]);
}
static ERL_NIF_TERM hold521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int limit, step, released;
    if (argc != 2 || !enif_get_int(env, argv[0], &limit) || limit < 1 || limit > 10000)
        return enif_make_badarg(env);
    increment521(&busy521);
    for (step = 0; step < limit && !read521(&released521); step++) pause521();
    released = read521(&released521);
    decrement521(&busy521);
    increment521(&done521);
    return enif_make_tuple3(env, enif_make_int(env, enif_thread_type()), argv[1], enif_make_int(env, released));
}
static ERL_NIF_TERM status521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argv;
    if (argc != 0) return enif_make_badarg(env);
    return enif_make_tuple6(env, enif_make_int(env, enif_thread_type()),
        enif_make_int(env, read521(&busy521)), enif_make_int(env, read521(&done521)),
        enif_make_int(env, read521(&released521)), enif_make_int(env, read521(&cpu_calls521)),
        enif_make_int(env, read521(&io_calls521)));
}
static ERL_NIF_TERM release521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argv;
    if (argc != 0) return enif_make_badarg(env);
    write521(&released521, 1);
    return enif_make_int(env, enif_thread_type());
}
static ERL_NIF_TERM reset521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argv;
    if (argc != 0 || read521(&busy521)) return enif_make_badarg(env);
    write521(&done521, 0); write521(&released521, 0);
    write521(&cpu_calls521, 0); write521(&io_calls521, 0);
    return atom521(env, "ok");
}

static int write_file521(const char *path, const char *text)
{
    char temporary[CONTROL_PATH_CAP521 + 16];
    FILE *stream;
    size_t length = strlen(text), written;
    int closed;
    snprintf(temporary, sizeof(temporary), "%s.tmp", path);
    stream = fopen(temporary, "wb");
    if (stream == NULL) return 0;
    written = fwrite(text, 1, length, stream);
    closed = fclose(stream);
    if (written != length || closed != 0) return 0;
    return rename(temporary, path) == 0;
}
static int file_released521(const char *path)
{
    FILE *stream = fopen(path, "rb");
    int first;
    if (stream == NULL) return 0;
    first = fgetc(stream);
    fclose(stream);
    return first == '1';
}
static int path521(ErlNifEnv *env, ERL_NIF_TERM value, char *output)
{
    int length = enif_get_string(env, value, output, CONTROL_PATH_CAP521, ERL_NIF_LATIN1);
    int index;
    if (length <= 1 || length >= CONTROL_PATH_CAP521 - 16) return 0;
    for (index = 0; index < length - 1; index++)
        if ((unsigned char)output[index] < 1 || (unsigned char)output[index] > 127) return 0;
    return 1;
}
static int load521(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    control521 *control = enif_alloc(sizeof(*control));
    const ERL_NIF_TERM *values;
    int arity, zero;
    if (control == NULL) return 1;
    memset(control, 0, sizeof(*control));
    init521(&control->active, 0); init521(&control->finished, 0);
    if (!(enif_get_int(env, info, &zero) && zero == 0)) {
        if (!enif_get_tuple(env, info, &arity, &values) || arity != 2 ||
            !path521(env, values[0], control->barrier) || !path521(env, values[1], control->marker)) {
            enif_free(control); return 1;
        }
        snprintf(control->entered, sizeof(control->entered), "%s.entered", control->barrier);
        snprintf(control->done, sizeof(control->done), "%s.done", control->barrier);
    }
    *private_data = control;
    return 0;
}
static void unload521(ErlNifEnv *env, void *private_data)
{
    control521 *control = private_data;
    char text[80];
    (void)env;
    if (control == NULL) return;
    if (control->marker[0] != 0) {
        snprintf(text, sizeof(text), "unloaded:%d:%d\n", read521(&control->active), read521(&control->finished));
        (void)write_file521(control->marker, text);
    }
    enif_free(control);
}
static ERL_NIF_TERM hold_control521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    control521 *control = enif_priv_data(env);
    int limit, step, released;
    char entered[48];
    if (argc != 2 || control == NULL || control->barrier[0] == 0 ||
        !enif_get_int(env, argv[0], &limit) || limit < 1 || limit > 10000)
        return enif_make_badarg(env);
    increment521(&control->active);
    snprintf(entered, sizeof(entered), "entered:%d\n", enif_thread_type());
    if (!write_file521(control->entered, entered)) {
        decrement521(&control->active);
        return enif_make_tuple2(env, atom521(env, "control_io_failed521"), enif_make_int(env, 1));
    }
    for (step = 0; step < limit && !file_released521(control->barrier); step++) pause521();
    released = file_released521(control->barrier);
    (void)write_file521(control->done, released ? "released\n" : "expired\n");
    decrement521(&control->active);
    increment521(&control->finished);
    return enif_make_tuple3(env, enif_make_int(env, enif_thread_type()), argv[1], enif_make_int(env, released));
}
typedef struct { flag521 done; int type; } foreign521;
#ifdef _WIN32
static DWORD WINAPI foreign_body521(LPVOID argument)
#else
static void *foreign_body521(void *argument)
#endif
{
    foreign521 *state = argument;
    state->type = enif_thread_type();
    write521(&state->done, 1);
    return 0;
}
static ERL_NIF_TERM foreign_thread521(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    foreign521 *state;
    int kind, created, joined;
#ifdef _WIN32
    HANDLE thread;
#else
    pthread_t thread;
#endif
    (void)argv;
    if (argc != 0) return enif_make_badarg(env);
    state = enif_alloc(sizeof(*state));
    if (state == NULL) return enif_make_badarg(env);
    init521(&state->done, 0); state->type = -1;
#ifdef _WIN32
    thread = CreateThread(NULL, 0, foreign_body521, state, 0, NULL);
    created = thread == NULL ? (int)GetLastError() : 0;
#else
    created = pthread_create(&thread, NULL, foreign_body521, state);
#endif
    if (created != 0) {
        enif_free(state);
        return enif_make_tuple2(env, atom521(env, "thread_create_failed521"), enif_make_int(env, created));
    }
    /* This helper owns/join its real OS thread. No NIF thread-creation API is tested. */
    while (!read521(&state->done)) pause521();
#ifdef _WIN32
    joined = WaitForSingleObject(thread, INFINITE) == WAIT_OBJECT_0 ? 0 : (int)GetLastError();
    CloseHandle(thread);
#else
    joined = pthread_join(thread, NULL);
#endif
    kind = state->type;
    enif_free(state);
    if (joined != 0) {
        /* Infrastructure failure: never return/unload with an unjoined helper. */
        fprintf(stderr, "dirty521 foreign thread join failed: %d\n", joined);
        abort();
    }
    return enif_make_tuple2(env, enif_make_int(env, enif_thread_type()), enif_make_int(env, kind));
}

static ErlNifFunc functions521[] = {
    {"hold_control_cpu", 2, hold_control521, ERL_NIF_DIRTY_JOB_CPU_BOUND},
    {"hold_control_io", 2, hold_control521, ERL_NIF_DIRTY_JOB_IO_BOUND},
    {"foreign_thread", 0, foreign_thread521, 0},
    {"normal", 1, observe521, 0},
    {"cpu", 1, observe521, CPU_FLAGS521},
    {"io", 1, observe521, ERL_NIF_DIRTY_JOB_IO_BOUND},
    {"hold_cpu", 2, hold521, ERL_NIF_DIRTY_JOB_CPU_BOUND},
    {"hold_io", 2, hold521, ERL_NIF_DIRTY_JOB_IO_BOUND},
    {"status", 0, status521, 0}, {"release", 0, release521, 0}, {"reset", 0, reset521, 0}
};
ERL_NIF_INIT(dirty_flags521, functions521, load521, 0, 0, unload521)
