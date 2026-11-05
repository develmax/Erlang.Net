/* Target C ABI/OS preflight only; this does not start a managed runtime. */
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include "erl_nif.h"

extern int nif_bridge_platform_layout(unsigned int, size_t *, unsigned int);
extern int nif_bridge_platform_open_file(const char *);
extern int nif_bridge_platform_getcwd(char *, size_t);
extern void *nif_bridge_platform_open(const char *, int);
extern void *nif_bridge_platform_symbol(void *, const char *);
extern int nif_bridge_platform_close(void *);

#define CHECK(condition) do { if (!(condition)) { fprintf(stderr, "failed: %s\n", #condition); return 1; } } while (0)
int main(int argc, char **argv)
{
    CHECK(argc == 2);
    size_t values[13] = {0};
    CHECK(nif_bridge_platform_layout(1, values, 13) == 0);
    CHECK(values[0] == sizeof(void *) && values[0] == 8);
    CHECK(values[1] == sizeof(long) && values[1] == 8);
    CHECK(values[2] == sizeof(ErlNifEntry) && values[2] == 96);
    CHECK(values[3] == sizeof(ErlNifFunc) && values[3] == 32);
    CHECK(values[4] == sizeof(ErlNifBinary) && values[4] == 40);
    CHECK(values[5] == 496);
    CHECK(nif_bridge_platform_layout(0, values, 13) != 0);
    CHECK(nif_bridge_platform_open_file("/nif-platform-absent-file") < 0);
    char buffer[65536];
    CHECK(nif_bridge_platform_getcwd(buffer, sizeof(buffer)) == 0);
    CHECK(buffer[0] == '/');
    CHECK(nif_bridge_platform_getcwd(buffer, 1) < 0);
    CHECK(nif_bridge_platform_open("/nif-platform-absent-library.so", 0) == NULL);
    void *library = nif_bridge_platform_open(argv[1], 1);
    CHECK(library != NULL);
    CHECK(nif_bridge_platform_symbol(library, "enif_alloc") != NULL);
    CHECK(nif_bridge_platform_symbol(library, "nif_bridge_platform_layout") != NULL);
    CHECK(nif_bridge_platform_close(library) == 0);
    puts("{\"Passed\":true,\"PointerBytes\":8,\"LongBytes\":8,\"ManagedRuntimeExecuted\":false}");
    return 0;
}
