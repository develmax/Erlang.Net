/* OTP-style Unix loader. Compile on the target host; OS constants stay in C. */
#define _POSIX_C_SOURCE 200809L
#include <dlfcn.h>
#include <fcntl.h>
#include <unistd.h>
#include <stddef.h>

#define PLATFORM_EXPORT __attribute__((visibility("default")))

PLATFORM_EXPORT void *nif_bridge_platform_open(const char *path, int global)
{
    dlerror();
    return dlopen(path, RTLD_NOW | (global ? RTLD_GLOBAL : RTLD_LOCAL));
}

PLATFORM_EXPORT void *nif_bridge_platform_symbol(void *handle, const char *name)
{
    dlerror();
    return dlsym(handle == NULL ? RTLD_DEFAULT : handle, name);
}

PLATFORM_EXPORT int nif_bridge_platform_close(void *handle)
{
    return dlclose(handle);
}

PLATFORM_EXPORT const char *nif_bridge_platform_error(void)
{
    return dlerror();
}

/* Return negative errno explicitly: reverse-P/Invoke does not preserve errno. */
#include <errno.h>
PLATFORM_EXPORT int nif_bridge_platform_open_file(const char *path)
{
    int descriptor = open(path, O_RDONLY | O_CLOEXEC);
    return descriptor < 0 ? -errno : descriptor;
}

PLATFORM_EXPORT int nif_bridge_platform_getcwd(char *buffer, size_t size)
{
    return getcwd(buffer, size) == NULL ? -errno : 0;
}
