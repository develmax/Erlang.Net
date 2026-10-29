#include <dlfcn.h>
#include <stdio.h>
int main(int argc, char **argv)
{
    if (argc != 2) return 10;
    void *image = dlopen(argv[1], RTLD_LAZY | RTLD_LOCAL);
    if (!image) { fprintf(stderr, "%s\n", dlerror()); return 20; }
    dlerror();
    int (*probe)(void) = (int (*)(void))dlsym(image, "path_value");
    const char *error = dlerror();
    if (error) { fprintf(stderr, "%s\n", error); dlclose(image); return 21; }
    int value = probe();
    printf("%d\n", value);
    return dlclose(image) == 0 ? 0 : 22;
}
