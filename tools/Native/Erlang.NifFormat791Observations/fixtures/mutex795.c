#include <erl_nif.h>
#include <errno.h>
#include <string.h>
#include <stdio.h>

static void phase795(const char *phase, int value)
{
    printf("mutex795 phase=%s value=%d\n", phase, value);
    fflush(stdout);
}
#ifdef _WIN32
#include <windows.h>
#include <process.h>
#else
#include <pthread.h>
#include <stdatomic.h>
#include <sched.h>
#endif

static ErlNifMutex *mutex795;
static const unsigned char expected_name[] = {'m','u','t','-',0xcf,0x80,0};
struct worker795 {
    int busy;
    int acquired;
    int created;
#ifdef _WIN32
    volatile LONG ready;
#else
    atomic_int ready;
#endif
};

static void work795(struct worker795 *work)
{
    phase795("worker-before-create", 0);
    ErlNifMutex *own = enif_mutex_create("native-worker795");
    phase795("worker-after-create", own != NULL);
    work->created = own != NULL;
    if (own != NULL) enif_mutex_destroy(own);
    phase795("worker-before-try", 0);
    work->busy = enif_mutex_trylock(mutex795);
    phase795("worker-after-try-raw", work->busy);
    if (work->busy == 0) {
        enif_mutex_unlock(mutex795);
        phase795("worker-balanced-successful-try", 0);
    }
#ifdef _WIN32
    InterlockedExchange(&work->ready, 1);
#else
    atomic_store_explicit(&work->ready, 1, memory_order_release);
#endif
    phase795("worker-before-lock", 0);
    enif_mutex_lock(mutex795);
    phase795("worker-after-lock", 0);
    work->acquired = 1;
    enif_mutex_unlock(mutex795);
    phase795("worker-after-unlock", 0);
}

#ifdef _WIN32
static unsigned __stdcall thread795(void *argument) { work795(argument); return 0; }
#else
static void *thread795(void *argument) { work795(argument); return NULL; }
#endif

static ERL_NIF_TERM test795(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    struct worker795 work = {0};
    ERL_NIF_TERM values[8];
    int first, second, unknown_name;
    ErlNifMutex *unknown;
    (void)argv;
    if (argc != 0 || mutex795 == NULL) return enif_make_badarg(env);
    phase795("main-before-first-try", 0);
    first = enif_mutex_trylock(mutex795);
    phase795("main-after-first-try-raw", first);
    if (first != 0) return enif_make_badarg(env);
    phase795("main-before-second-try", 0);
    second = enif_mutex_trylock(mutex795);
    phase795("main-after-second-try-raw", second);
    if (second == 0) {
        enif_mutex_unlock(mutex795);
        phase795("main-balanced-second-success", 0);
    }
#ifdef _WIN32
    uintptr_t thread = _beginthreadex(NULL, 0, thread795, &work, 0, NULL);
    if (!thread) { enif_mutex_unlock(mutex795); return enif_make_badarg(env); }
    phase795("main-before-ready-wait", 0);
    while (!InterlockedCompareExchange(&work.ready, 0, 0)) SwitchToThread();
    phase795("main-after-ready-wait", 0);
    enif_mutex_unlock(mutex795);
    phase795("main-after-unlock-before-join", 0);
    if (WaitForSingleObject((HANDLE)thread, INFINITE) != WAIT_OBJECT_0) {
        CloseHandle((HANDLE)thread); return enif_make_badarg(env);
    }
    CloseHandle((HANDLE)thread);
    phase795("main-after-join", 0);
#else
    pthread_t thread;
    if (pthread_create(&thread, NULL, thread795, &work) != 0) {
        enif_mutex_unlock(mutex795); return enif_make_badarg(env);
    }
    while (!atomic_load_explicit(&work.ready, memory_order_acquire)) sched_yield();
    enif_mutex_unlock(mutex795);
    if (pthread_join(thread, NULL) != 0) return enif_make_badarg(env);
#endif
    phase795("main-before-null-name-create", 0);
    unknown = enif_mutex_create(NULL);
    phase795("main-after-null-name-create", unknown != NULL);
    if (unknown == NULL) return enif_make_badarg(env);
    unknown_name = strcmp(enif_mutex_name(unknown), "unknown") == 0;
    enif_mutex_destroy(unknown);
    values[0] = enif_make_int(env, first == 0);
    values[1] = enif_make_int(env, second == EBUSY);
    values[2] = enif_make_int(env, work.busy == EBUSY);
    values[3] = enif_make_int(env, work.acquired);
    values[4] = enif_make_int(env, memcmp(enif_mutex_name(mutex795), expected_name, sizeof(expected_name)) == 0);
    values[5] = enif_make_int(env, unknown_name);
    values[6] = enif_make_int(env, enif_mutex_name(NULL) == NULL);
    values[7] = enif_make_int(env, work.created);
    phase795("main-before-result", 0);
    return enif_make_tuple_from_array(env, values, 8);
}

static int load795(ErlNifEnv *env, void **priv, ERL_NIF_TERM info)
{
    unsigned char name[sizeof(expected_name)];
    (void)env; (void)priv; (void)info;
    memcpy(name, expected_name, sizeof(name));
    mutex795 = enif_mutex_create((char*)name);
    name[0] = 'X'; /* The API must copy the original name bytes. */
    if (mutex795 == NULL) return 1;
#ifdef MUTEX795_FAIL_LOAD
    return 1; /* Host failed-init containment: no unload callback is promised. */
#else
    return 0;
#endif
}

static void unload795(ErlNifEnv *env, void *priv)
{
    (void)env; (void)priv;
    if (mutex795 != NULL) { enif_mutex_destroy(mutex795); mutex795 = NULL; }
}

static ErlNifFunc functions795[] = { {"test", 0, test795, 0} };
ERL_NIF_INIT(mutex795, functions795, load795, NULL, NULL, unload795)
