/* Main-executable exports for OTP-linked Mach-O bundles; no ERTS is embedded. */
#include <dlfcn.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include "hostfxr.h"

__attribute__((visibility("default"))) uint32_t nif_bridge_process_host(void)
{
    return 1;
}

int main(int argc, char **argv)
{
    if (argc < 4) {
        fprintf(stderr, "usage: erlang-nif-host <libhostfxr> <dotnet-root> <app.dll> [arguments...]\n");
        return 64;
    }
    void *fxr = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (fxr == NULL) {
        fprintf(stderr, "%s\n", dlerror());
        return 70;
    }
    hostfxr_main_startupinfo_fn start = (hostfxr_main_startupinfo_fn)dlsym(fxr, "hostfxr_main_startupinfo");
    if (start == NULL) {
        fprintf(stderr, "%s\n", dlerror());
        return 70;
    }
    const char **arguments = calloc((size_t)argc, sizeof(*arguments));
    if (arguments == NULL)
        return 71;
    arguments[0] = argv[0];
    for (int index = 4; index < argc; index++)
        arguments[index - 3] = argv[index];
    int result = start(argc - 3, arguments, argv[0], argv[2], argv[3]);
    free(arguments);
    /* hostfxr and the main executable own process-lifetime native callbacks. */
    return result;
}
