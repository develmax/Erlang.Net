#define _GNU_SOURCE
#include <dlfcn.h>
#include <link.h>
#include <stdint.h>
#include <stdio.h>
#include <stdatomic.h>
typedef uintptr_t (*allocator_fn)(void);
typedef int (*register_fn)(allocator_fn);
typedef uintptr_t (*read_fn)(void);
static _Atomic(unsigned) allocations[2];
static uintptr_t allocate_first(void) { atomic_fetch_add(&allocations[0],1); return 300; }
static uintptr_t allocate_second(void) { atomic_fetch_add(&allocations[1],1); return 301; }
int main(int argc,char **argv)
{
    void *bridges[2]={NULL,NULL},*capsules[2]={NULL,NULL};
    Lmid_t namespaces[2]={LM_ID_BASE,LM_ID_BASE};
    allocator_fn allocators[2]={allocate_first,allocate_second};
    read_fn constructors[2]={NULL,NULL},live[2]={NULL,NULL};
    int i,result=1;
    if (argc!=3) return 2;
    for (i=0;i<2;++i) {
        register_fn reg;
        Lmid_t actual_namespace=LM_ID_BASE;
        bridges[i]=dlmopen(LM_ID_NEWLM,argv[1],RTLD_NOW|RTLD_LOCAL);
        if (!bridges[i]) { fprintf(stderr,"bridge[%d]: %s\n",i,dlerror()); goto cleanup; }
        if (dlinfo(bridges[i],RTLD_DI_LMID,&namespaces[i])) goto cleanup;
        reg=(register_fn)dlsym(bridges[i],"probe_register_allocator");
        if (!reg || reg(allocators[i])) { fprintf(stderr,"register[%d] failed\n",i); goto cleanup; }
        if (atomic_load(&allocations[i])!=0) goto cleanup;
        capsules[i]=dlmopen(namespaces[i],argv[2],RTLD_NOW|RTLD_LOCAL);
        if (!capsules[i]) { fprintf(stderr,"capsule[%d] namespace=%ld: %s\n",i,(long)namespaces[i],dlerror()); goto cleanup; }
        if (dlinfo(capsules[i],RTLD_DI_LMID,&actual_namespace) || actual_namespace!=namespaces[i]) goto cleanup;
        constructors[i]=(read_fn)dlsym(capsules[i],"probe_read_constructor_domain");
        live[i]=(read_fn)dlsym(capsules[i],"probe_read_live_domain");
        if (!constructors[i] || !live[i]) goto cleanup;
        printf("constructor[%d] namespace=%ld expected=%d captured=%lu allocations=%u\n",i,(long)namespaces[i],300+i,(unsigned long)constructors[i](),atomic_load(&allocations[i]));
        if (constructors[i]()!=(uintptr_t)(300+i) || atomic_load(&allocations[i])!=1) goto cleanup;
    }
    if (namespaces[0]==LM_ID_BASE || namespaces[1]==LM_ID_BASE || namespaces[0]==namespaces[1]) goto cleanup;
    for (i=0;i<2;++i) {
        uintptr_t saved=constructors[i](),current=live[i]();
        printf("reread[%d] namespace=%ld expected=%d constructor=%lu live=%lu allocations=%u\n",i,(long)namespaces[i],300+i,(unsigned long)saved,(unsigned long)current,atomic_load(&allocations[i]));
        if (saved!=(uintptr_t)(300+i) || current!=(uintptr_t)(300+i) || atomic_load(&allocations[i])!=2) goto cleanup;
    }
    result=0;
cleanup:
    for (i=1;i>=0;--i) { if (capsules[i]) dlclose(capsules[i]); if (bridges[i]) dlclose(bridges[i]); }
    return result;
}
