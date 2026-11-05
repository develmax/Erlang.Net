#include <stdint.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>
#include <errno.h>

typedef uintptr_t term;
typedef int (*writer)(void *, const char *, size_t);
typedef struct {
    uint32_t version, size, maximum_tuple, maximum_list;
    term (*tuple)(void *, const term *, unsigned);
    term (*list)(void *, const term *, unsigned);
    term (*limit)(void *);
    int (*format)(uintptr_t, int64_t, writer, void *);
} callbacks;

__declspec(dllimport) int nif_windows_varargs_register526(const callbacks *);
__declspec(dllimport) term nif_windows_make_tuple526(void *, unsigned, ...);
__declspec(dllimport) term nif_windows_make_list526(void *, unsigned, ...);
__declspec(dllimport) int nif_windows_snprintf791(char *, size_t, const char *, ...);
__declspec(dllimport) int nif_windows_set_option(void *, int, ...);

static term collect(void *env, const term *values, unsigned count)
{
    unsigned i;
    (void)env;
    for (i = 0; i < count; i++)
        if (values[i] != (term)(i + 1)) return 0;
    return (term)(count + 1);
}

static term limit(void *env) { (void)env; return UINTPTR_MAX; }
static int format(uintptr_t value, int64_t depth, writer write, void *state)
{
    (void)value; (void)depth; (void)write; (void)state;
    return 0;
}

int main(void)
{
    callbacks table = {2, sizeof(callbacks), 32, 32, collect, collect, limit, format};
    char output[128];
    if (nif_windows_varargs_register526(&table) != 0) return 1;
    if (nif_windows_make_tuple526(NULL, 17,
        (term)1, (term)2, (term)3, (term)4, (term)5, (term)6,
        (term)7, (term)8, (term)9, (term)10, (term)11, (term)12,
        (term)13, (term)14, (term)15, (term)16, (term)17) != 18) return 2;
    if (nif_windows_make_list526(NULL, 9,
        (term)1, (term)2, (term)3, (term)4, (term)5,
        (term)6, (term)7, (term)8, (term)9) != 10) return 3;
    if (nif_windows_make_tuple526(NULL, 0) != 1) return 4;
    if (nif_windows_make_list526(NULL, 33) != UINTPTR_MAX) return 5;
    if (nif_windows_set_option(NULL, 1, (void *)NULL) != EINVAL) return 6;
    if (nif_windows_snprintf791(output, sizeof(output), "%lld:%u:%s",
        (long long)-9223372036854775807LL, (unsigned)42, "ok") != 26) return 7;
    if (strcmp(output, "-9223372036854775807:42:ok") != 0) return 8;
    puts("native varargs: tuple17/list9/empty/limit/set_option/format passed");
    return 0;
}
