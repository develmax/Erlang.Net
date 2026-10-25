#define STATIC_ERLANG_DRIVER 1
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>
#include <stdint.h>
#include <stdlib.h>
#include <errno.h>
#include <limits.h>
#include <erl_driver.h>
#include <erl_win_dyn_driver.h>

/* Genuine own native services, not CLR thread termination or NIF aliases.
 * The private host composes these 29 typed callbacks with its other services;
 * this partial vector MUST NOT be passed to a driver's driver_init. */

typedef int (__cdecl *drv420_reserve)(uint64_t owner, uint64_t *reservation);
typedef int (__cdecl *drv420_publish)(uint64_t reservation, void *thread);
typedef void (__cdecl *drv420_abort)(uint64_t reservation);
static drv420_reserve reserve_worker;
static drv420_publish publish_worker;
static drv420_abort abort_worker;
static INIT_ONCE initialized = INIT_ONCE_STATIC_INIT;
static DWORD self_slot = FLS_OUT_OF_INDEXES;
static DWORD owner_slot = TLS_OUT_OF_INDEXES;

struct ErlDrvMutex_ { SRWLOCK lock; };
struct ErlDrvCond_ { CONDITION_VARIABLE condition; };
struct ErlDrvRWLock_ { SRWLOCK lock; };
struct ErlDrvTid_ {
    HANDLE handle;
    HANDLE start_gate;
    DWORD id;
    LONG owned;
    LONG run_user;
    LONG joining;
    void *result;
    void *(*function)(void *);
    void *argument;
    uint64_t owner;
    uint64_t reservation;
};

static void fatal_argument(void) { abort(); }
static void *allocate(size_t size) { return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, size); }
static void release(void *value) {
    if (value && !HeapFree(GetProcessHeap(), 0, value)) fatal_argument();
}
static VOID WINAPI destroy_external_self(PVOID value) {
    struct ErlDrvTid_ *tid = (struct ErlDrvTid_*)value;
    if (tid && !tid->owned) release(tid);
}
static BOOL CALLBACK initialize(PINIT_ONCE once, PVOID parameter, PVOID *context) {
    (void)once; (void)parameter; (void)context;
    self_slot = FlsAlloc(destroy_external_self);
    if (self_slot == FLS_OUT_OF_INDEXES) return FALSE;
    owner_slot = TlsAlloc();
    if (owner_slot == TLS_OUT_OF_INDEXES) {
        FlsFree(self_slot); self_slot = FLS_OUT_OF_INDEXES; return FALSE;
    }
    return TRUE;
}
static int ready(void) { return InitOnceExecuteOnce(&initialized, initialize, NULL, NULL) != 0; }

__declspec(dllexport) int __cdecl erldrv420_configure_threads(
    drv420_reserve reserve, drv420_publish publish, drv420_abort cancel) {
    if (!reserve || !publish || !cancel || !ready()) return EINVAL;
    /* Configure once, before any image can execute. Rebinding a live native
     * callback table to another VM is forbidden even inside one child. */
    if (reserve_worker || publish_worker || abort_worker) return EBUSY;
    reserve_worker = reserve; publish_worker = publish; abort_worker = cancel;
    return 0;
}
__declspec(dllexport) uint64_t __cdecl erldrv420_exchange_owner(uint64_t owner) {
    uint64_t old;
    if (!ready()) fatal_argument();
    old = (uint64_t)(uintptr_t)TlsGetValue(owner_slot);
    if (!TlsSetValue(owner_slot, (void*)(uintptr_t)owner)) fatal_argument();
    return old;
}

static ErlDrvMutex *mutex_create(char *name) {
    ErlDrvMutex *value = allocate(sizeof(*value)); (void)name;
    if (value) InitializeSRWLock(&value->lock);
    return value;
}
static void mutex_destroy(ErlDrvMutex *value) { if (!value) fatal_argument(); release(value); }
static int mutex_trylock(ErlDrvMutex *value) {
    if (!value) fatal_argument(); return TryAcquireSRWLockExclusive(&value->lock) ? 0 : EBUSY;
}
static void mutex_lock(ErlDrvMutex *value) { if (!value) fatal_argument(); AcquireSRWLockExclusive(&value->lock); }
static void mutex_unlock(ErlDrvMutex *value) { if (!value) fatal_argument(); ReleaseSRWLockExclusive(&value->lock); }
static ErlDrvCond *condition_create(char *name) {
    ErlDrvCond *value = allocate(sizeof(*value)); (void)name;
    if (value) InitializeConditionVariable(&value->condition);
    return value;
}
static void condition_destroy(ErlDrvCond *value) { if (!value) fatal_argument(); release(value); }
static void condition_signal(ErlDrvCond *value) { if (!value) fatal_argument(); WakeConditionVariable(&value->condition); }
static void condition_broadcast(ErlDrvCond *value) { if (!value) fatal_argument(); WakeAllConditionVariable(&value->condition); }
static void condition_wait(ErlDrvCond *value, ErlDrvMutex *mutex) {
    if (!value || !mutex) fatal_argument();
    if (!SleepConditionVariableSRW(&value->condition, &mutex->lock, INFINITE, 0)) fatal_argument();
}
static ErlDrvRWLock *rwlock_create(char *name) {
    ErlDrvRWLock *value = allocate(sizeof(*value)); (void)name;
    if (value) InitializeSRWLock(&value->lock);
    return value;
}
static void rwlock_destroy(ErlDrvRWLock *value) { if (!value) fatal_argument(); release(value); }
static int rwlock_tryread(ErlDrvRWLock *value) {
    if (!value) fatal_argument(); return TryAcquireSRWLockShared(&value->lock) ? 0 : EBUSY;
}
static void rwlock_read(ErlDrvRWLock *value) { if (!value) fatal_argument(); AcquireSRWLockShared(&value->lock); }
static void rwlock_unread(ErlDrvRWLock *value) { if (!value) fatal_argument(); ReleaseSRWLockShared(&value->lock); }
static int rwlock_trywrite(ErlDrvRWLock *value) {
    if (!value) fatal_argument(); return TryAcquireSRWLockExclusive(&value->lock) ? 0 : EBUSY;
}
static void rwlock_write(ErlDrvRWLock *value) { if (!value) fatal_argument(); AcquireSRWLockExclusive(&value->lock); }
static void rwlock_unwrite(ErlDrvRWLock *value) { if (!value) fatal_argument(); ReleaseSRWLockExclusive(&value->lock); }
static int tsd_create(char *name, ErlDrvTSDKey *key) {
    DWORD slot; (void)name;
    if (!key) fatal_argument();
    slot = TlsAlloc(); if (slot == TLS_OUT_OF_INDEXES) return ENOMEM;
    if (slot > INT_MAX) { TlsFree(slot); return ENOMEM; }
    *key = (ErlDrvTSDKey)slot; return 0;
}
static void tsd_destroy(ErlDrvTSDKey key) { if (key < 0 || !TlsFree((DWORD)key)) fatal_argument(); }
static void tsd_set(ErlDrvTSDKey key, void *value) { if (key < 0 || !TlsSetValue((DWORD)key,value)) fatal_argument(); }
static void *tsd_get(ErlDrvTSDKey key) {
    void *value; if (key < 0) fatal_argument();
    value = TlsGetValue((DWORD)key);
    if (!value && GetLastError() != ERROR_SUCCESS) fatal_argument();
    return value;
}
static ErlDrvThreadOpts *options_create(char *name) {
    ErlDrvThreadOpts *value = allocate(sizeof(*value)); (void)name;
    if (value) value->suggested_stack_size = -1;
    return value;
}
static void options_destroy(ErlDrvThreadOpts *value) { if (!value) fatal_argument(); release(value); }

static unsigned __stdcall thread_entry(void *argument) {
    struct ErlDrvTid_ *tid = argument;
    struct ErlDrvTid_ *previous = FlsGetValue(self_slot);
    if (previous && !previous->owned) release(previous);
    if (!FlsSetValue(self_slot,tid)) fatal_argument();
    erldrv420_exchange_owner(tid->owner);
    if (WaitForSingleObject(tid->start_gate,INFINITE) != WAIT_OBJECT_0) fatal_argument();
    if (!CloseHandle(tid->start_gate)) fatal_argument(); tid->start_gate = NULL;
    if (InterlockedCompareExchange(&tid->run_user,0,0)) tid->result = tid->function(tid->argument);
    /* No worker-release callback here. CRT/native TLS destructors can still
     * execute after this return. The host observes actual OS thread exit. */
    return 0;
}
static int thread_create(char *name, ErlDrvTid *output, void *(*function)(void*), void *argument, ErlDrvThreadOpts *options) {
    struct ErlDrvTid_ *tid;
    uintptr_t handle;
    size_t stack_size = 0;
    int error; (void)name;
    if (!output || !function) fatal_argument();
    if (!ready() || !reserve_worker || !publish_worker || !abort_worker) return EINVAL;
    tid = allocate(sizeof(*tid)); if (!tid) return ENOMEM;
    tid->owned=1; tid->function=function; tid->argument=argument;
    tid->owner=(uint64_t)(uintptr_t)TlsGetValue(owner_slot);
    if (!tid->owner) { release(tid); return EINVAL; }
    tid->start_gate=CreateEventW(NULL,TRUE,FALSE,NULL);
    if (!tid->start_gate) { release(tid); return ENOMEM; }
    error=reserve_worker(tid->owner,&tid->reservation);
    if (error) { CloseHandle(tid->start_gate); release(tid); return error; }
    if (options && options->suggested_stack_size >= 0) {
        /* Public option is a suggestion in kilowords, not bytes. Let the real
         * Windows loader apply its own native stack reservation/page minimum. */
        size_t words=(size_t)options->suggested_stack_size;
        if (words > UINT_MAX / (1024u*sizeof(void*))) {
            abort_worker(tid->reservation); CloseHandle(tid->start_gate); release(tid); return EINVAL;
        }
        stack_size=words*1024u*sizeof(void*);
    }
    handle=_beginthreadex(NULL,(unsigned)stack_size,thread_entry,tid,0,(unsigned*)&tid->id);
    if (!handle) {
        error=errno ? errno : EAGAIN;
        abort_worker(tid->reservation); CloseHandle(tid->start_gate); release(tid); return error;
    }
    tid->handle=(HANDLE)handle;
    error=publish_worker(tid->reservation,tid->handle);
    if (error) {
        /* The user function has not run. Still join the actual OS thread before
         * abandoning the reservation, including all native TLS destruction. */
        if (!SetEvent(tid->start_gate)) fatal_argument();
        if (WaitForSingleObject(tid->handle,INFINITE)!=WAIT_OBJECT_0) fatal_argument();
        CloseHandle(tid->handle); abort_worker(tid->reservation); release(tid); return error;
    }
    *output=tid;
    InterlockedExchange(&tid->run_user,1);
    if (!SetEvent(tid->start_gate)) fatal_argument();
    return 0;
}
static ErlDrvTid thread_self(void) {
    struct ErlDrvTid_ *tid;
    if (!ready()) fatal_argument();
    tid=FlsGetValue(self_slot);
    if (!tid) {
        tid=allocate(sizeof(*tid)); if (!tid) fatal_argument();
        tid->id=GetCurrentThreadId();
        if (!FlsSetValue(self_slot,tid)) { release(tid); fatal_argument(); }
    }
    return tid;
}
static int equal_threads(ErlDrvTid left, ErlDrvTid right) {
    if (!left || !right) fatal_argument();
    return left->id == right->id;
}
static void thread_exit(void *result) {
    struct ErlDrvTid_ *tid;
    if (!ready()) fatal_argument(); tid=FlsGetValue(self_slot);
    if (!tid || !tid->owned) fatal_argument();
    tid->result=result;
    _endthreadex(0);
}
static int thread_join(ErlDrvTid tid, void **result) {
    if (!tid || !tid->owned) return EINVAL;
    if (tid->id == GetCurrentThreadId()) return EDEADLK;
    if (InterlockedCompareExchange(&tid->joining,1,0)) return EINVAL;
    if (WaitForSingleObject(tid->handle,INFINITE) != WAIT_OBJECT_0) {
        InterlockedExchange(&tid->joining,0); return EINVAL;
    }
    if (result) *result=tid->result;
    if (!CloseHandle(tid->handle)) fatal_argument();
    release(tid); return 0;
}

__declspec(dllexport) int __cdecl erldrv420_thread_callbacks(TWinDynDriverCallbacks *table, size_t bytes) {
    if (!table || bytes != sizeof(*table)) return EINVAL;
    table->erl_drv_mutex_create=mutex_create; table->erl_drv_mutex_destroy=mutex_destroy;
    table->erl_drv_mutex_trylock=mutex_trylock; table->erl_drv_mutex_lock=mutex_lock; table->erl_drv_mutex_unlock=mutex_unlock;
    table->erl_drv_cond_create=condition_create; table->erl_drv_cond_destroy=condition_destroy;
    table->erl_drv_cond_signal=condition_signal; table->erl_drv_cond_broadcast=condition_broadcast; table->erl_drv_cond_wait=condition_wait;
    table->erl_drv_rwlock_create=rwlock_create; table->erl_drv_rwlock_destroy=rwlock_destroy;
    table->erl_drv_rwlock_tryrlock=rwlock_tryread; table->erl_drv_rwlock_rlock=rwlock_read; table->erl_drv_rwlock_runlock=rwlock_unread;
    table->erl_drv_rwlock_tryrwlock=rwlock_trywrite; table->erl_drv_rwlock_rwlock=rwlock_write; table->erl_drv_rwlock_rwunlock=rwlock_unwrite;
    table->erl_drv_tsd_key_create=tsd_create; table->erl_drv_tsd_key_destroy=tsd_destroy; table->erl_drv_tsd_set=tsd_set; table->erl_drv_tsd_get=tsd_get;
    table->erl_drv_thread_opts_create=options_create; table->erl_drv_thread_opts_destroy=options_destroy;
    table->erl_drv_thread_create=thread_create; table->erl_drv_thread_self=thread_self; table->erl_drv_equal_tids=equal_threads;
    table->erl_drv_thread_exit=thread_exit; table->erl_drv_thread_join=thread_join;
    return 0;
}

/* Private host calls this only after every entry/port invocation and native
 * worker has physically drained. FlsFree also runs external-thread destructors
 * before this DLL can be unmapped; no CLR worker may keep a stale FLS callback. */
__declspec(dllexport) int __cdecl erldrv420_close_threads(void) {
    if (self_slot != FLS_OUT_OF_INDEXES) {
        if (!FlsFree(self_slot)) return (int)GetLastError();
        self_slot=FLS_OUT_OF_INDEXES;
    }
    if (owner_slot != TLS_OUT_OF_INDEXES) {
        if (!TlsFree(owner_slot)) return (int)GetLastError();
        owner_slot=TLS_OUT_OF_INDEXES;
    }
    return 0;
}
