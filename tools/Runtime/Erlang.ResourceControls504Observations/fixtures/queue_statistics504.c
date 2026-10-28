#include "erl_nif.h"
#include <pthread.h>
#include <time.h>

static pthread_mutex_t gate = PTHREAD_MUTEX_INITIALIZER;
static pthread_cond_t ready = PTHREAD_COND_INITIALIZER;
static int entered[2], completed[2], timeouts[2], released, release_calls;

static ERL_NIF_TERM hold_kind(ErlNifEnv *env, int kind)
{
    struct timespec deadline;
    int result = 0;
    clock_gettime(CLOCK_REALTIME, &deadline);
    deadline.tv_sec += 20;
    pthread_mutex_lock(&gate);
    entered[kind]++;
    while (!released && result == 0)
        result = pthread_cond_timedwait(&ready, &gate, &deadline);
    if (result != 0) timeouts[kind]++;
    completed[kind]++;
    pthread_mutex_unlock(&gate);
    return enif_make_atom(env, result == 0 ? "released" : "bounded_timeout");
}

static ERL_NIF_TERM cpu(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return hold_kind(env, 0);
}

static ERL_NIF_TERM io(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return hold_kind(env, 1);
}

static ERL_NIF_TERM release(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    pthread_mutex_lock(&gate);
    release_calls++;
    released = 1;
    pthread_cond_broadcast(&ready);
    pthread_mutex_unlock(&gate);
    return enif_make_atom(env, "ok");
}

static ERL_NIF_TERM status(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM result;
    (void)argc; (void)argv;
    pthread_mutex_lock(&gate);
    result = enif_make_tuple4(env,
        enif_make_int(env, entered[0]), enif_make_int(env, entered[1]),
        enif_make_int(env, completed[0]), enif_make_int(env, completed[1]));
    pthread_mutex_unlock(&gate);
    return result;
}

static ERL_NIF_TERM details(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM fields[8];
    (void)argc; (void)argv;
    pthread_mutex_lock(&gate);
    fields[0] = enif_make_int(env, entered[0]);
    fields[1] = enif_make_int(env, entered[1]);
    fields[2] = enif_make_int(env, completed[0]);
    fields[3] = enif_make_int(env, completed[1]);
    fields[4] = enif_make_int(env, timeouts[0]);
    fields[5] = enif_make_int(env, timeouts[1]);
    fields[6] = enif_make_int(env, released);
    fields[7] = enif_make_int(env, release_calls);
    pthread_mutex_unlock(&gate);
    return enif_make_tuple_from_array(env, fields, 8);
}

static ErlNifFunc functions[] = {
    {"cpu", 0, cpu, ERL_NIF_DIRTY_JOB_CPU_BOUND},
    {"io", 0, io, ERL_NIF_DIRTY_JOB_IO_BOUND},
    {"release", 0, release, 0},
    {"status", 0, status, 0},
    {"details", 0, details, 0}
};
ERL_NIF_INIT(queue_statistics504, functions, NULL, NULL, NULL, NULL)
