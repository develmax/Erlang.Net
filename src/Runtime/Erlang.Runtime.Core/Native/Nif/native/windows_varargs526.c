#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0600
#endif
#include <windows.h>
#include <stdint.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include "nif_snprintf791.h"

#define EXPORT526 __declspec(dllexport)
#define VERSION526 UINT32_C(2)

typedef uintptr_t term526;
typedef term526 (__cdecl *array526)(void *, const term526 *, unsigned);
typedef term526 (__cdecl *limit526)(void *);

typedef struct {
    uint32_t version;
    uint32_t size;
    uint32_t maximum_tuple;
    uint32_t maximum_list;
    array526 tuple;
    array526 list;
    limit526 system_limit;
    nif_printf_term791 format_term791;
} callbacks526;

_Static_assert(sizeof(void *) == 8, "Windows x64 adapter only");
_Static_assert(sizeof(callbacks526) == 48, "Managed registration layout mismatch");

static SRWLOCK publication526 = SRWLOCK_INIT;
static callbacks526 callbacks;
static int registered;

EXPORT526 int __cdecl nif_windows_varargs_register526(const callbacks526 *input)
{
    int result = 0;
    if (!input || input->version != VERSION526 || input->size != sizeof(callbacks526)
        || !input->tuple || !input->list || !input->system_limit || !input->format_term791) {
        return -1;
    }

    AcquireSRWLockExclusive(&publication526);
    if (registered) {
        if (memcmp(&callbacks, input, sizeof(callbacks526)) != 0) {
            result = -2;
        }
    } else {
        callbacks = *input;
        registered = 1;
    }
    ReleaseSRWLockExclusive(&publication526);

    return result;
}

static term526 make526(void *env, unsigned count, va_list arguments, int tuple)
{
    term526 *items;
    term526 result;
    unsigned index;
    if (!registered) {
        return 0;
    }
    if (count > (tuple ? callbacks.maximum_tuple : callbacks.maximum_list)) {
        return callbacks.system_limit(env);
    }
    if (!count) {
        return (tuple ? callbacks.tuple : callbacks.list)(env, NULL, 0);
    }

    items = (term526 *)malloc((size_t)count * sizeof(term526));
    if (!items) {
        return callbacks.system_limit(env);
    }
    for (index = 0; index < count; index++) {
        items[index] = va_arg(arguments, term526);
    }
    result = (tuple ? callbacks.tuple : callbacks.list)(env, items, count);
    free(items);

    return result;
}

EXPORT526 term526 __cdecl nif_windows_make_tuple526(void *env, unsigned count, ...)
{
    term526 result;
    va_list arguments;
    va_start(arguments, count);
    result = make526(env, count, arguments, 1);
    va_end(arguments);

    return result;
}

EXPORT526 term526 __cdecl nif_windows_make_list526(void *env, unsigned count, ...)
{
    term526 result;
    va_list arguments;
    va_start(arguments, count);
    result = make526(env, count, arguments, 0);
    va_end(arguments);

    return result;
}

EXPORT526 int __cdecl nif_windows_snprintf791(char *buffer, size_t size, const char *format, ...)
{
    int result;
    va_list arguments;
    if (!registered) return -EINVAL;
    va_start(arguments, format);
    result = nif_vsnprintf791(buffer, size, format, arguments, callbacks.format_term791);
    va_end(arguments);

    return result;
}
