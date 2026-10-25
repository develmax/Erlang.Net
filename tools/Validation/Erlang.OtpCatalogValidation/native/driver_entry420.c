#define STATIC_ERLANG_DRIVER 1
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <errno.h>
#include <stdlib.h>
#include <erl_driver.h>

typedef ErlDrvData (__cdecl *drv420_start)(ErlDrvPort, char*);

/* Capture the driver CRT errno in the same native invocation before any CLR
 * transition, diagnostics or another P/Invoke can overwrite this thread state. */
__declspec(dllexport) ErlDrvData __cdecl erldrv420_start_entry(
    drv420_start start, ErlDrvPort port, char *command, int *native_errno) {
    ErlDrvData result;
    int error;
    if (!start || !native_errno) abort();
    result=start(port,command);
    error=errno;
    *native_errno=error;
    return result;
}
