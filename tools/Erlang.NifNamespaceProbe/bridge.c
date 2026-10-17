#define _GNU_SOURCE
#include <dlfcn.h>
#include <stdint.h>
static uintptr_t domain;
void probe_register(uintptr_t value) { domain=value; }
uintptr_t probe_domain_ref(void) { return domain; }
void *probe_open(const char *path) { return dlopen(path,RTLD_NOW|RTLD_LOCAL); }
const char *probe_error(void) { return dlerror(); }
