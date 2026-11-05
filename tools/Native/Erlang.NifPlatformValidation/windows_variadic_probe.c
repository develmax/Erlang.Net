#include <stdint.h>
#include <stddef.h>

typedef uintptr_t (__cdecl *make_probe)(void *, unsigned, ...);
typedef int (__cdecl *format_probe)(char *, size_t, const char *, ...);

__declspec(dllexport) uintptr_t __cdecl nif_windows_variadic_probe(make_probe make, void *env, unsigned count, const uintptr_t *v)
{
    if (count == 17)
        return make(env, 17, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8], v[9], v[10], v[11], v[12], v[13], v[14], v[15], v[16]);
    if (count == 9)
        return make(env, 9, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8]);
    return 0;
}

__declspec(dllexport) int __cdecl nif_windows_format_probe(format_probe format, uintptr_t term)
{
    char buffer[8];
    return format(buffer, sizeof(buffer), "%.7T", term);
}
