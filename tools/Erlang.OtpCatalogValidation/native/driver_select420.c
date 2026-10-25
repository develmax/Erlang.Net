#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>
#include <stdint.h>
#include <stdlib.h>

typedef void (__cdecl *erldrv420_select_signal)(void*, int);
typedef struct erldrv420_selection {
    HANDLE event;
    HANDLE cancel;
    HANDLE thread;
    erldrv420_select_signal signal;
    void *token;
} erldrv420_selection;

static unsigned __stdcall erldrv420_selection_main(void *argument) {
    erldrv420_selection *selection=(erldrv420_selection*)argument;
    HANDLE wait[2];
    DWORD result, error;
    wait[0]=selection->cancel;
    wait[1]=selection->event;
    result=WaitForMultipleObjects(2,wait,FALSE,INFINITE);
    if (result==WAIT_OBJECT_0+1) selection->signal(selection->token,0);
    else if (result!=WAIT_OBJECT_0) {
        error=result==WAIT_FAILED ? GetLastError() : ERROR_INVALID_HANDLE;
        selection->signal(selection->token,(int)error);
    }
    return 0;
}

/* Host-private poll worker. This is not exported in the ErlDrv callback table.
 * The original HANDLE is borrowed until cancel returns after physical join. */
__declspec(dllexport) int __cdecl erldrv420_select_create(
    HANDLE event, erldrv420_select_signal signal, void *token, void **result) {
    erldrv420_selection *selection;
    uintptr_t thread;
    DWORD error;
    if (!signal || !result) return ERROR_INVALID_PARAMETER;
    *result=NULL;
    selection=(erldrv420_selection*)calloc(1,sizeof(*selection));
    if (!selection) return ERROR_NOT_ENOUGH_MEMORY;
    selection->event=event;
    selection->signal=signal;
    selection->token=token;
    selection->cancel=CreateEventW(NULL,TRUE,FALSE,NULL);
    if (!selection->cancel) { error=GetLastError(); free(selection); return (int)error; }
    thread=_beginthreadex(NULL,0,erldrv420_selection_main,selection,0,NULL);
    if (!thread) { CloseHandle(selection->cancel); free(selection); return ERROR_NOT_ENOUGH_MEMORY; }
    selection->thread=(HANDLE)thread;
    *result=selection;
    return 0;
}

__declspec(dllexport) int __cdecl erldrv420_select_cancel(void *opaque) {
    erldrv420_selection *selection=(erldrv420_selection*)opaque;
    DWORD result, error=0;
    if (!selection) return ERROR_INVALID_PARAMETER;
    if (GetThreadId(selection->thread)==GetCurrentThreadId()) return ERROR_POSSIBLE_DEADLOCK;
    if (!SetEvent(selection->cancel)) return (int)GetLastError();
    result=WaitForSingleObject(selection->thread,INFINITE);
    if (result!=WAIT_OBJECT_0) return result==WAIT_FAILED ? (int)GetLastError() : ERROR_INVALID_HANDLE;
    /* After this physical fence no native wait or reverse callback can use the
     * borrowed HANDLE, token, managed delegate, or host image code. */
    if (!CloseHandle(selection->thread)) error=GetLastError();
    if (!CloseHandle(selection->cancel) && !error) error=GetLastError();
    free(selection);
    return (int)error;
}
