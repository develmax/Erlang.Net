#define _GNU_SOURCE
#include <dlfcn.h>
#include <link.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
typedef void (*register_fn)(uintptr_t);
typedef void *(*open_fn)(const char *);
typedef const char *(*error_fn)(void);
typedef uintptr_t (*read_fn)(void);
static int run(const char *bridge,const char *entry)
{
    void *bridges[2]={NULL,NULL},*entries[2]={NULL,NULL};
    Lmid_t namespaces[2]={LM_ID_BASE,LM_ID_BASE};
    uintptr_t actual[2]={0,0};
    read_fn readers[2]={NULL,NULL};
    int i,result=1;
    for (i=0;i<2;++i) {
        register_fn reg;
        open_fn open_entry;
        error_fn error;
        read_fn read;
        bridges[i]=dlmopen(LM_ID_NEWLM,bridge,RTLD_NOW|RTLD_LOCAL);
        if (!bridges[i]) { fprintf(stderr,"bridge[%d]: %s\n",i,dlerror()); goto cleanup; }
        if (dlinfo(bridges[i],RTLD_DI_LMID,&namespaces[i])) goto cleanup;
        reg=(register_fn)dlsym(bridges[i],"probe_register");
        open_entry=(open_fn)dlsym(bridges[i],"probe_open");
        error=(error_fn)dlsym(bridges[i],"probe_error");
        if (!reg || !open_entry || !error) goto cleanup;
        reg((uintptr_t)(100+i));
        entries[i]=open_entry(entry);
        if (!entries[i]) { fprintf(stderr,"entry[%d] namespace=%ld: %s\n",i,(long)namespaces[i],error()); goto cleanup; }
        read=(read_fn)dlsym(entries[i],"probe_read_domain");
        if (!read) goto cleanup;
        readers[i]=read;
        actual[i]=read();
        printf("domain[%d] namespace=%ld expected=%d actual=%lu\n",i,(long)namespaces[i],100+i,(unsigned long)actual[i]);
    }
    for (i=0;i<2;++i) {
        actual[i]=readers[i]();
        printf("reread-domain[%d] namespace=%ld actual=%lu\n",i,(long)namespaces[i],(unsigned long)actual[i]);
    }
    if (namespaces[0]!=LM_ID_BASE && namespaces[1]!=LM_ID_BASE && namespaces[0]!=namespaces[1] && actual[0]==100 && actual[1]==101) result=0;
cleanup:
    for (i=1;i>=0;--i) { if (entries[i]) dlclose(entries[i]); if (bridges[i]) dlclose(bridges[i]); }
    return result;
}
static int run_capsule(const char *path)
{
    void *capsules[2]={NULL,NULL};
    Lmid_t namespaces[2]={LM_ID_BASE,LM_ID_BASE};
    uintptr_t actual[2]={0,0};
    read_fn readers[2]={NULL,NULL};
    int i,result=1;
    for (i=0;i<2;++i) {
        register_fn reg;
        read_fn read;
        capsules[i]=dlmopen(LM_ID_NEWLM,path,RTLD_NOW|RTLD_LOCAL);
        if (!capsules[i]) { fprintf(stderr,"capsule[%d]: %s\n",i,dlerror()); goto cleanup; }
        if (dlinfo(capsules[i],RTLD_DI_LMID,&namespaces[i])) goto cleanup;
        reg=(register_fn)dlsym(capsules[i],"probe_register");
        read=(read_fn)dlsym(capsules[i],"probe_read_domain");
        if (!reg || !read) { fprintf(stderr,"capsule symbols[%d]: %s\n",i,dlerror()); goto cleanup; }
        reg((uintptr_t)(200+i));
        readers[i]=read;
        actual[i]=read();
        printf("capsule-domain[%d] namespace=%ld expected=%d actual=%lu\n",i,(long)namespaces[i],200+i,(unsigned long)actual[i]);
    }
    for (i=0;i<2;++i) {
        actual[i]=readers[i]();
        printf("reread-domain[%d] namespace=%ld actual=%lu\n",i,(long)namespaces[i],(unsigned long)actual[i]);
    }
    if (namespaces[0]!=LM_ID_BASE && namespaces[1]!=LM_ID_BASE && namespaces[0]!=namespaces[1] && actual[0]==200 && actual[1]==201) result=0;
cleanup:
    for (i=1;i>=0;--i) if (capsules[i]) dlclose(capsules[i]);
    return result;
}
int main(int argc,char **argv)
{
    if (argc==2) return run_capsule(argv[1]);
    if (argc==3) return run(argv[1],argv[2]);
    return 2;
}
