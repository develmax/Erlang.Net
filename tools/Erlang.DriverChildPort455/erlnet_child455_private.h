#ifndef ERLNET_CHILD455_PRIVATE_H
#define ERLNET_CHILD455_PRIVATE_H
#include "erl_driver.h"
#include <stdint.h>

/* Separate PRIVATE ABI. Do not add fields to ErlDrvEntry/WinDyn callbacks. */
#define ERLNET_CHILD455_ABI UINT32_C(45501)
#if defined(ERLNET_CHILD455_IMPLEMENTATION)
#define ERLNET_CHILD455_LINK __declspec(dllexport)
#else
#define ERLNET_CHILD455_LINK __declspec(dllimport)
#endif

typedef ErlDrvPort (__cdecl *erlnet_child455_create_fn)(
    ErlDrvPort creator, ErlDrvTermData authenticated_pid,
    const char *name, ErlDrvData caller_owned_data);
typedef int (__cdecl *erlnet_child455_internal_pid_fn)(ErlDrvTermData token);

typedef struct {
    uint32_t abi;
    uint32_t size;
    erlnet_child455_create_fn create;
    erlnet_child455_internal_pid_fn internal_pid;
} erlnet_child455_callbacks;

ERLNET_CHILD455_LINK int __cdecl erlnet_child455_configure(
    const erlnet_child455_callbacks *callbacks);
ERLNET_CHILD455_LINK uint32_t __cdecl erlnet_child455_abi(void);
ERLNET_CHILD455_LINK ErlDrvPort __cdecl erlnet_child455_create(
    ErlDrvPort creator, ErlDrvTermData pid, const char *name, ErlDrvData data);
ERLNET_CHILD455_LINK int __cdecl erlnet_child455_is_not_internal_pid(
    ErlDrvTermData token);
#endif
