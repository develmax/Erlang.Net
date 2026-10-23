#define STATIC_ERLANG_DRIVER 1
#define ERLNET_CHILD455_IMPLEMENTATION
#include "erlnet_child455_private.h"
#include <windows.h>

/* One host engine per dedicated native host. Configured once before any image
   is admitted; engine roots delegates until images, ports and jobs drain.
   No clear/reconfigure export: shutdown must close that host process/session. */
static erlnet_child455_callbacks installed455;
static volatile LONG state455;

uint32_t __cdecl erlnet_child455_abi(void) { return ERLNET_CHILD455_ABI; }

int __cdecl erlnet_child455_configure(const erlnet_child455_callbacks *c) {
    if (c == NULL || c->abi != ERLNET_CHILD455_ABI ||
            c->size != sizeof(*c) || c->create == NULL || c->internal_pid == NULL)
        return ERROR_INVALID_PARAMETER;
    if (InterlockedCompareExchange(&state455,1,0) != 0)
        return ERROR_ALREADY_INITIALIZED;
    installed455=*c;
    InterlockedExchange(&state455,2);
    return ERROR_SUCCESS;
}

ErlDrvPort __cdecl erlnet_child455_create(ErlDrvPort creator,
    ErlDrvTermData pid,const char *name,ErlDrvData data) {
    if (InterlockedCompareExchange(&state455,2,2) != 2)
        return (ErlDrvPort)-1; /* Explicit unconfigured refusal, never success. */
    return installed455.create(creator,pid,name,data);
}

int __cdecl erlnet_child455_is_not_internal_pid(ErlDrvTermData token) {
    if (InterlockedCompareExchange(&state455,2,2) != 2) return 1;
    return installed455.internal_pid(token) ? 0 : 1;
}
