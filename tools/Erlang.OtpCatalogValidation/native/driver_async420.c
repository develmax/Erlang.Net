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
} drv420_async_lane;

typedef struct drv420_async_pool {
    SRWLOCK gate;
    unsigned count;
    int closing;
    drv420_async_complete complete;
    drv420_async_lane lanes[1];
} drv420_async_pool;

static unsigned __stdcall drv420_async_run(void *argument) {
    drv420_async_lane *lane=(drv420_async_lane*)argument;
    for (;;) {
        drv420_async_job *job;
        uint64_t previous;
        AcquireSRWLockExclusive(&lane->gate);
        while (!lane->head && !lane->closing)
            if (!SleepConditionVariableSRW(&lane->changed,&lane->gate,INFINITE,0)) abort();
        job=lane->head;
        if (!job) {
            ReleaseSRWLockExclusive(&lane->gate);
            return 0;
        }
        lane->head=job->next;
        if (!lane->head) lane->tail=NULL;
        ReleaseSRWLockExclusive(&lane->gate);
        previous=erldrv420_exchange_owner(job->owner);
        job->invoke(job->data);
        erldrv420_exchange_owner(previous);
        lane->pool->complete(job->identity);
        if (!HeapFree(GetProcessHeap(),0,job)) abort();
    }
}

__declspec(dllexport) int __cdecl erldrv420_async_create(
    unsigned count, drv420_async_complete complete, drv420_async_pool **output) {
    drv420_async_pool *pool;
    size_t bytes;
    unsigned index;
    int failure=0;
    if (!count || !complete || !output) return EINVAL;
    if ((size_t)count-1 > (SIZE_MAX-sizeof(*pool))/sizeof(drv420_async_lane)) return ENOMEM;
    bytes=sizeof(*pool)+((size_t)count-1)*sizeof(drv420_async_lane);
    pool=(drv420_async_pool*)HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,bytes);
    if (!pool) return ENOMEM;
    InitializeSRWLock(&pool->gate); pool->count=count; pool->complete=complete;
    for (index=0;index<count;index++) {
        drv420_async_lane *lane=&pool->lanes[index];
        uintptr_t thread;
        lane->pool=pool;
        InitializeSRWLock(&lane->gate); InitializeConditionVariable(&lane->changed);
        thread=_beginthreadex(NULL,0,drv420_async_run,lane,0,(unsigned*)&lane->thread_id);
        if (!thread) { failure=errno ? errno : EAGAIN; break; }
        lane->thread=(HANDLE)thread;
    }
    if (!failure) { *output=pool; return 0; }
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
