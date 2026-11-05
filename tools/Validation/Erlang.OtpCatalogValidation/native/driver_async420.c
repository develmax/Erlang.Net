#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>
#include <stdint.h>
#include <stdlib.h>
#include <errno.h>

/* Persistent physical keyed lanes, not Task.Run or a fresh thread per job.
 * Completion only announces return from invoke; it is NOT a TLS-exit proof.
 * Real pool close drains accepted jobs then joins every actual OS thread. */
typedef void (__cdecl *drv420_async_invoke)(void*);
typedef void (__cdecl *drv420_async_complete)(uint64_t);
extern uint64_t __cdecl erldrv420_exchange_owner(uint64_t);

typedef struct drv420_async_job {
    struct drv420_async_job *next;
    uint64_t owner;
    uint64_t identity;
    drv420_async_invoke invoke;
    void *data;
} drv420_async_job;

enum { DRV1126_AUX, DRV1126_CHECK_IO, DRV1126_EMULATOR, DRV1126_GC,
       DRV1126_OTHER, DRV1126_PORT, DRV1126_SLEEP, DRV1126_STATES };
enum { DRV1126_ENABLE, DRV1126_DISABLE, DRV1126_RESET, DRV1126_GATHER };
typedef struct drv1126_record {
    SRWLOCK gate;
    unsigned identity, os_thread, state;
    uint64_t perf, counters[DRV1126_STATES];
} drv1126_record;
typedef struct drv1126_row {
    uint32_t version, identity, os_thread, state;
    uint64_t frequency, perf, counters[DRV1126_STATES];
} drv1126_row;
struct drv420_async_pool;
typedef struct drv420_async_lane {
    struct drv420_async_pool *pool;
    SRWLOCK gate;
    CONDITION_VARIABLE changed;
    HANDLE thread;
    DWORD thread_id;
    drv420_async_job *head;
    drv420_async_job *tail;
    int closing;
    drv1126_record accounting;
} drv420_async_lane;

typedef struct drv420_async_pool {
    SRWLOCK gate;
    unsigned count;
    int closing;
    int accounting1126;
    volatile LONG enabled1126;
    unsigned initialized1126;
    CONDITION_VARIABLE initialized_changed1126;
    uint64_t frequency1126;
    uint64_t qpc_frequency1139;
    drv420_async_complete complete;
    drv420_async_lane lanes[1];
} drv420_async_pool;

/* Exact unsigned split-word algorithm from pinned erl_time.h200-229.
 * Convert EACH timestamp before subtraction, not accumulated counter words. */
static uint64_t drv1139_convert(uint64_t value, uint32_t from) {
    const uint32_t to=1000000000;
    uint64_t high, low, result;
    if (value<=UINT64_MAX/to) return (value*to)/from;
    low=value & UINT64_C(0xffffffff);
    high=(value>>32) & UINT64_C(0xffffffff);
    low*=to; high*=to;
    high+=(low>>32) & UINT64_C(0xffffffff);
    low&=UINT64_C(0xffffffff);
    result=high%from; high/=from; high<<=32;
    result<<=32; result+=low; result/=from; result+=high;
    return result;
}
static uint64_t drv1126_now(drv420_async_pool *pool) {
    LARGE_INTEGER value;
    if (!QueryPerformanceCounter(&value) || value.QuadPart<=0) abort();
    return drv1139_convert((uint64_t)value.QuadPart,(uint32_t)pool->qpc_frequency1139);
}
static int drv1126_cache(drv420_async_lane *lane) {
    return lane->pool->accounting1126 &&
        InterlockedCompareExchange(&lane->pool->enabled1126,0,0)!=0;
}
/* Cached callers keep their cache across invoke/wait just as ERTS does.
 * No record mutex spans arbitrary driver code, condition wait or callback. */
static unsigned drv1126_state(drv420_async_lane *lane, unsigned next) {
    drv1126_record *record=&lane->accounting;
    unsigned previous;
    AcquireSRWLockExclusive(&record->gate);
    previous=record->state;
    if (record->perf && next!=previous) {
        uint64_t now=drv1126_now(lane->pool);
        record->counters[previous]+=now-record->perf;
        record->perf=now;
    }
    record->state=next;
    ReleaseSRWLockExclusive(&record->gate);
    return previous;
}
static void drv1126_register(drv420_async_lane *lane) {
    drv420_async_pool *pool=lane->pool;
    if (!pool->accounting1126) return;
    InitializeSRWLock(&lane->accounting.gate);
    lane->accounting.os_thread=GetCurrentThreadId();
    lane->accounting.state=DRV1126_OTHER;
    /* perf/counters remain zero even if some parent VM enabled MSACC earlier. */
    AcquireSRWLockExclusive(&pool->gate);
    lane->accounting.identity=pool->initialized1126++;
    WakeAllConditionVariable(&pool->initialized_changed1126);
    ReleaseSRWLockExclusive(&pool->gate);
}
static unsigned __stdcall drv420_async_run(void *argument) {
    drv420_async_lane *lane=(drv420_async_lane*)argument;
    drv1126_register(lane);
    for (;;) {
        drv420_async_job *job;
        uint64_t previous;
        AcquireSRWLockExclusive(&lane->gate);
        while (!lane->head && !lane->closing) {
            int cached=drv1126_cache(lane);
            unsigned prior=cached ? drv1126_state(lane,DRV1126_SLEEP) : DRV1126_OTHER;
            if (!SleepConditionVariableSRW(&lane->changed,&lane->gate,INFINITE,0)) abort();
            if (cached) drv1126_state(lane,prior);
        }
        job=lane->head;
        if (!job) {
            ReleaseSRWLockExclusive(&lane->gate);
            return 0;
        }
        lane->head=job->next;
        if (!lane->head) lane->tail=NULL;
        ReleaseSRWLockExclusive(&lane->gate);
        previous=erldrv420_exchange_owner(job->owner);
        {
            int cached=drv1126_cache(lane);
            if (cached) drv1126_state(lane,DRV1126_PORT);
            job->invoke(job->data);
            if (cached) drv1126_state(lane,DRV1126_OTHER);
        }
        erldrv420_exchange_owner(previous);
        lane->pool->complete(job->identity);
        if (!HeapFree(GetProcessHeap(),0,job)) abort();
    }
}

static int drv420_async_create_core(
    unsigned count, drv420_async_complete complete, drv420_async_pool **output,
    int accounting1126) {
    drv420_async_pool *pool;
    size_t bytes;
    unsigned index;
    int failure=0;
    LARGE_INTEGER frequency={0};
    if (!count || !complete || !output) return EINVAL;
    if (accounting1126 && (!QueryPerformanceFrequency(&frequency) || frequency.QuadPart<=0 || (uint64_t)frequency.QuadPart>UINT32_MAX)) return EINVAL;
    *output=NULL;
    if ((size_t)count-1 > (SIZE_MAX-sizeof(*pool))/sizeof(drv420_async_lane)) return ENOMEM;
    bytes=sizeof(*pool)+((size_t)count-1)*sizeof(drv420_async_lane);
    pool=(drv420_async_pool*)HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,bytes);
    if (!pool) return ENOMEM;
    InitializeSRWLock(&pool->gate); pool->count=count; pool->complete=complete;
    pool->accounting1126=accounting1126;
    pool->frequency1126=accounting1126 ? UINT64_C(1000000000) : 0;
    pool->qpc_frequency1139=accounting1126 ? (uint64_t)frequency.QuadPart : 0;
    InitializeConditionVariable(&pool->initialized_changed1126);
    for (index=0;index<count;index++) {
        drv420_async_lane *lane=&pool->lanes[index];
        uintptr_t thread;
        lane->pool=pool;
        InitializeSRWLock(&lane->gate); InitializeConditionVariable(&lane->changed);
        thread=_beginthreadex(NULL,0,drv420_async_run,lane,0,(unsigned*)&lane->thread_id);
        if (!thread) { failure=errno ? errno : EAGAIN; break; }
        lane->thread=(HANDLE)thread;
    }
    if (!failure) {
        if (accounting1126) {
            AcquireSRWLockExclusive(&pool->gate);
            while (pool->initialized1126!=count)
                if (!SleepConditionVariableSRW(&pool->initialized_changed1126,&pool->gate,INFINITE,0)) abort();
            ReleaseSRWLockExclusive(&pool->gate);
        }
        *output=pool; return 0;
    }
    while (index) {
        drv420_async_lane *lane=&pool->lanes[--index];
        AcquireSRWLockExclusive(&lane->gate); lane->closing=1;
        WakeAllConditionVariable(&lane->changed); ReleaseSRWLockExclusive(&lane->gate);
        if (WaitForSingleObject(lane->thread,INFINITE)!=WAIT_OBJECT_0) abort();
        if (!CloseHandle(lane->thread)) abort();
    }
    if (!HeapFree(GetProcessHeap(),0,pool)) abort();
    return failure;
}

/* Existing ABI keeps its original no-accounting behavior. */
__declspec(dllexport) int __cdecl erldrv420_async_create(
    unsigned count, drv420_async_complete complete, drv420_async_pool **output) {
    return drv420_async_create_core(count,complete,output,0);
}
__declspec(dllexport) int __cdecl erldrv420_async_create_msacc1126(
    unsigned count, drv420_async_complete complete, drv420_async_pool **output) {
    return drv420_async_create_core(count,complete,output,1);
}
/* Caller must hold the typed owner's REAL active-control lease until return.
 * A SafeHandle addref alone does not protect against explicit close. */
__declspec(dllexport) int __cdecl erldrv420_async_msacc_info1126(
    drv420_async_pool *pool, uint32_t *count, uint64_t *frequency) {
    if (!pool || !pool->accounting1126 || !count || !frequency) return EINVAL;
    *count=pool->count; *frequency=pool->frequency1126; return 0;
}
/* Provenance stays separate from the public nanosecond counter unit. */
__declspec(dllexport) int __cdecl erldrv420_async_msacc_qpc1139(
    drv420_async_pool *pool, uint64_t *frequency) {
    if (!pool || !pool->accounting1126 || !frequency) return EINVAL;
    *frequency=pool->qpc_frequency1139; return 0;
}
/* Pure production clock conversion, also callable by boundary controls. */
__declspec(dllexport) int __cdecl erldrv420_async_msacc_convert1139(
    uint64_t value, uint32_t from, uint64_t *result) {
    if (!from || !result) return EINVAL;
    *result=drv1139_convert(value,from); return 0;
}
__declspec(dllexport) int __cdecl erldrv420_async_msacc_control1126(
    drv420_async_pool *pool, unsigned action, unsigned identity, drv1126_row *row) {
    drv1126_record *record=NULL;
    unsigned index;
    if (!pool || !pool->accounting1126 || action>DRV1126_GATHER || identity>=pool->count || !row) return EINVAL;
    for (index=0;index<pool->count;index++)
        if (pool->lanes[index].accounting.identity==identity) { record=&pool->lanes[index].accounting; break; }
    if (!record) return EINVAL;
    AcquireSRWLockExclusive(&record->gate);
    switch (action) {
    case DRV1126_ENABLE: record->perf=drv1126_now(pool); record->state=DRV1126_SLEEP; break;
    case DRV1126_DISABLE:
        record->counters[record->state]+=drv1126_now(pool)-record->perf;
        record->perf=0; break;
    case DRV1126_RESET: ZeroMemory(record->counters,sizeof(record->counters)); break;
    case DRV1126_GATHER:
        if (record->perf) {
            uint64_t now=drv1126_now(pool);
            record->counters[record->state]+=now-record->perf;
            record->perf=now;
        }
        break;
    }
    ReleaseSRWLockExclusive(&record->gate);
    /* Preserve source gather's update -> unlock -> actual snapshot lock window. */
    AcquireSRWLockExclusive(&record->gate);
    row->version=1126; row->identity=record->identity;
    row->os_thread=record->os_thread; row->state=record->state;
    row->frequency=pool->frequency1126; row->perf=record->perf;
    CopyMemory(row->counters,record->counters,sizeof(record->counters));
    ReleaseSRWLockExclusive(&record->gate);
    return 0;
}
/* Separate source-phase finish; never conflate requested and instrumented. */
__declspec(dllexport) int __cdecl erldrv420_async_msacc_finish1126(
    drv420_async_pool *pool, unsigned action) {
    if (!pool || !pool->accounting1126 || action>DRV1126_GATHER) return EINVAL;
    if (action==DRV1126_ENABLE) InterlockedExchange(&pool->enabled1126,1);
    else if (action==DRV1126_DISABLE) InterlockedExchange(&pool->enabled1126,0);
    return 0;
}
__declspec(dllexport) int __cdecl erldrv420_async_submit(
    drv420_async_pool *pool, unsigned lane_number, uint64_t owner, uint64_t identity,
    drv420_async_invoke invoke, void *data) {
    drv420_async_job *job;
    drv420_async_lane *lane;
    if (!pool || lane_number>=pool->count || !owner || !identity || !invoke) return EINVAL;
    job=(drv420_async_job*)HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,sizeof(*job));
    if (!job) return ENOMEM;
    job->owner=owner; job->identity=identity; job->invoke=invoke; job->data=data;
    AcquireSRWLockShared(&pool->gate);
    if (pool->closing) {
        ReleaseSRWLockShared(&pool->gate); HeapFree(GetProcessHeap(),0,job); return EINVAL;
    }
    lane=&pool->lanes[lane_number];
    AcquireSRWLockExclusive(&lane->gate);
    if (lane->tail) lane->tail->next=job; else lane->head=job;
    lane->tail=job;
    WakeConditionVariable(&lane->changed);
    ReleaseSRWLockExclusive(&lane->gate);
    ReleaseSRWLockShared(&pool->gate);
    return 0;
}

__declspec(dllexport) int __cdecl erldrv420_async_close(drv420_async_pool *pool) {
    unsigned index;
    if (!pool) return EINVAL;
    for (index=0;index<pool->count;index++)
        if (pool->lanes[index].thread_id==GetCurrentThreadId()) return EDEADLK;
    AcquireSRWLockExclusive(&pool->gate);
    if (pool->closing) { ReleaseSRWLockExclusive(&pool->gate); return EINVAL; }
    pool->closing=1;
    for (index=0;index<pool->count;index++) {
        drv420_async_lane *lane=&pool->lanes[index];
        AcquireSRWLockExclusive(&lane->gate); lane->closing=1;
        WakeAllConditionVariable(&lane->changed); ReleaseSRWLockExclusive(&lane->gate);
    }
    ReleaseSRWLockExclusive(&pool->gate);
    for (index=0;index<pool->count;index++) {
        HANDLE thread=pool->lanes[index].thread;
        if (WaitForSingleObject(thread,INFINITE)!=WAIT_OBJECT_0) return (int)GetLastError();
        if (!CloseHandle(thread)) return (int)GetLastError();
        pool->lanes[index].thread=NULL;
    }
    if (!HeapFree(GetProcessHeap(),0,pool)) return (int)GetLastError();
    return 0;
}
