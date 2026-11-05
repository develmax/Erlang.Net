#include "erl_driver.h"
#ifdef _WIN32
#include "erl_win_dyn_driver.h"
#endif
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <limits.h>

/* Original-only observer fixture: genuine persistent driver_async work.
 * It uses no managed-pool ABI, no fabricated accounting and no counter clock. */
enum { SUBMIT_HELD = 1, RELEASE = 2, SNAPSHOT = 3, SUBMIT_FREE = 4 };
typedef struct state1127 {
    ErlDrvPort port;
    ErlDrvMutex *gate;
    ErlDrvCond *changed;
    unsigned refs, jobs;
    int released;
    uintptr_t last_tid;
} state1127;
typedef struct job1127 { state1127 *state; int held; } job1127;
static ErlDrvMutex *totals_gate;
static unsigned long long invoked, returned, ready_count, freed, live_states, live_jobs;

static void drop_state(state1127 *s) {
    int destroy;
    erl_drv_mutex_lock(s->gate);
    destroy = --s->refs == 0;
    erl_drv_mutex_unlock(s->gate);
    if (destroy) {
        erl_drv_cond_destroy(s->changed);
        erl_drv_mutex_destroy(s->gate);
        erl_drv_mutex_lock(totals_gate);
        --live_states;
        erl_drv_mutex_unlock(totals_gate);
        driver_free(s);
    }
}
static void free_job(void *arg) {
    job1127 *job = (job1127 *)arg;
    state1127 *s = job->state;
    erl_drv_mutex_lock(s->gate);
    --s->jobs;
    erl_drv_mutex_unlock(s->gate);
    erl_drv_mutex_lock(totals_gate);
    ++freed;
    --live_jobs;
    erl_drv_mutex_unlock(totals_gate);
    driver_free(job);
    drop_state(s);
}
static void invoke_job(void *arg) {
    job1127 *job = (job1127 *)arg;
    state1127 *s = job->state;
    erl_drv_mutex_lock(s->gate);
    s->last_tid = (uintptr_t)erl_drv_thread_self();
    erl_drv_mutex_lock(totals_gate);
    ++invoked;
    erl_drv_mutex_unlock(totals_gate);
    while (job->held && !s->released)
        erl_drv_cond_wait(s->changed, s->gate);
    erl_drv_mutex_unlock(s->gate);
    erl_drv_mutex_lock(totals_gate);
    ++returned;
    erl_drv_mutex_unlock(totals_gate);
}
static void ready_job(ErlDrvData data, ErlDrvThreadData arg) {
    state1127 *s = (state1127 *)data;
    erl_drv_mutex_lock(totals_gate);
    ++ready_count;
    erl_drv_mutex_unlock(totals_gate);
    driver_output(s->port, "done", 4);
    free_job(arg);
}
static int initialize(void) {
    totals_gate = erl_drv_mutex_create("async1127_totals");
    if (!totals_gate) return -1;
    invoked = returned = ready_count = freed = live_states = live_jobs = 0;
    return 0;
}
static void finish(void) {
    /* Any outstanding native owner is a real cleanup failure. */
    int dirty;
    erl_drv_mutex_lock(totals_gate);
    dirty = live_jobs != 0 || live_states != 0;
    erl_drv_mutex_unlock(totals_gate);
    if (dirty) abort();
    erl_drv_mutex_destroy(totals_gate);
    totals_gate = NULL;
}
static ErlDrvData start(ErlDrvPort port, char *command) {
    state1127 *s = driver_alloc(sizeof(*s));
    (void)command;
    if (!s) return ERL_DRV_ERROR_GENERAL;
    memset(s, 0, sizeof(*s));
    s->port = port;
    s->refs = 1;
    s->gate = erl_drv_mutex_create("async1127_state");
    s->changed = erl_drv_cond_create("async1127_release");
    if (!s->gate || !s->changed) {
        if (s->changed) erl_drv_cond_destroy(s->changed);
        if (s->gate) erl_drv_mutex_destroy(s->gate);
        driver_free(s);
        return ERL_DRV_ERROR_GENERAL;
    }
    erl_drv_mutex_lock(totals_gate);
    ++live_states;
    erl_drv_mutex_unlock(totals_gate);
    return (ErlDrvData)s;
}
static void stop(ErlDrvData data) {
    state1127 *s = (state1127 *)data;
    erl_drv_mutex_lock(s->gate);
    s->released = 1;
    erl_drv_cond_broadcast(s->changed);
    erl_drv_mutex_unlock(s->gate);
    drop_state(s);
}
static ErlDrvSSizeT reply(const char *text, char **buffer, ErlDrvSizeT capacity) {
    size_t length = strlen(text);
    if (length > capacity) {
        *buffer = driver_alloc(length);
        if (!*buffer) return -1;
    }
    memcpy(*buffer, text, length);
    return (ErlDrvSSizeT)length;
}
static ErlDrvSSizeT control(ErlDrvData data, unsigned command, char *bytes,
                           ErlDrvSizeT length, char **buffer, ErlDrvSizeT capacity) {
    state1127 *s = (state1127 *)data;
    char out[256];
    if (command == SNAPSHOT) {
        erl_drv_mutex_lock(s->gate);
        erl_drv_mutex_lock(totals_gate);
        snprintf(out, sizeof(out), "%llu,%llu,%llu,%llu,%llu,%llu,%llu,%u,%d",
                 invoked, returned, ready_count, freed, live_states, live_jobs,
                 (unsigned long long)s->last_tid, s->jobs, s->released);
        erl_drv_mutex_unlock(totals_gate);
        erl_drv_mutex_unlock(s->gate);
        return reply(out, buffer, capacity);
    }
    if (command == RELEASE) {
        erl_drv_mutex_lock(s->gate);
        s->released = 1;
        erl_drv_cond_broadcast(s->changed);
        erl_drv_mutex_unlock(s->gate);
        return reply("released", buffer, capacity);
    }
    if (command == SUBMIT_HELD || command == SUBMIT_FREE) {
        char key_text[32], *end;
        unsigned long parsed;
        unsigned key;
        long id;
        job1127 *job;
        if (length == 0 || length >= sizeof(key_text)) return -1;
        memcpy(key_text, bytes, length);
        key_text[length] = 0;
        parsed = strtoul(key_text, &end, 10);
        if (*end || parsed > UINT_MAX) return -1;
        key = (unsigned)parsed;
        job = driver_alloc(sizeof(*job));
        if (!job) return -1;
        job->state = s;
        job->held = command == SUBMIT_HELD;
        erl_drv_mutex_lock(s->gate);
        ++s->refs;
        ++s->jobs;
        erl_drv_mutex_unlock(s->gate);
        erl_drv_mutex_lock(totals_gate);
        ++live_jobs;
        erl_drv_mutex_unlock(totals_gate);
        id = driver_async(s->port, &key, invoke_job, job, free_job);
        if (id < 0) free_job(job);
        snprintf(out, sizeof(out), "%ld", id);
        return reply(out, buffer, capacity);
    }
    return -1;
}
static ErlDrvEntry entry = {
    initialize, start, stop, NULL, NULL, NULL, "async_msacc1127_drv",
    finish, NULL, control, NULL, NULL, ready_job, NULL, NULL, NULL,
    ERL_DRV_EXTENDED_MARKER, ERL_DRV_EXTENDED_MAJOR_VERSION,
    ERL_DRV_EXTENDED_MINOR_VERSION, ERL_DRV_FLAG_USE_PORT_LOCKING, NULL, NULL
};
DRIVER_INIT(async_msacc1127_drv) { return &entry; }
