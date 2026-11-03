#include "erl_nif.h"
#include <stddef.h>
#ifndef CASE_MAJOR
#define CASE_MAJOR 2
#endif
#ifndef CASE_MINOR
#define CASE_MINOR 18
#endif
#ifndef CASE_OPTIONS
#define CASE_OPTIONS 0
#endif
#ifndef CASE_VARIANT
#define CASE_VARIANT "beam.vanilla"
#endif
#ifndef CASE_MODULE
#define CASE_MODULE "nif_version651"
#endif
#ifndef CASE_MINIMUM_ERTS
#define CASE_MINIMUM_ERTS "erts-version651"
#endif
/* Versioned physical tails; no modern struct masquerades as an old short one. */
typedef struct {
    const char *name; unsigned arity;
    ERL_NIF_TERM (*fptr)(ErlNifEnv*,int,const ERL_NIF_TERM[]);
#if CASE_MINOR >= 7 || CASE_MAJOR > 2
    unsigned flags;
#endif
} Function651;
typedef struct {
    int major, minor; const char *name; int count; Function651 *functions;
    int (*load)(ErlNifEnv*,void**,ERL_NIF_TERM);
    int (*reload)(ErlNifEnv*,void**,ERL_NIF_TERM);
    int (*upgrade)(ErlNifEnv*,void**,void**,ERL_NIF_TERM);
    void (*unload)(ErlNifEnv*,void*);
#if CASE_MINOR >= 1 || CASE_MAJOR > 2
    const char *variant;
#endif
#if CASE_MINOR >= 7 || CASE_MAJOR > 2
    unsigned options;
#endif
#if CASE_MINOR >= 12 || CASE_MAJOR > 2
    size_t resource_init_size;
#endif
#if CASE_MINOR >= 14 || CASE_MAJOR > 2
    const char *minimum_erts;
#endif
} Entry651;
static ERL_NIF_TERM identity651(ErlNifEnv *env,int argc,const ERL_NIF_TERM argv[])
{ (void)argc; (void)argv; return enif_make_int(env,42); }
static ERL_NIF_TERM second651(ErlNifEnv *env,int argc,const ERL_NIF_TERM argv[])
{ (void)env; (void)argc; return argv[0]; }
static Function651 functions651[] = {
    {"identity",0,identity651
#if CASE_MINOR >= 7 || CASE_MAJOR > 2
    ,0
#endif
    },
    {"second",1,second651
#if CASE_MINOR >= 7 || CASE_MAJOR > 2
    ,0
#endif
    }
};
ERL_NIF_INIT_GLOB
ERL_NIF_INIT_DECL(nif_version651);
ERL_NIF_INIT_DECL(nif_version651)
{
    static Entry651 entry651 = {
        CASE_MAJOR,CASE_MINOR,CASE_MODULE,2,functions651,0,0,0,0
#if CASE_MINOR >= 1 || CASE_MAJOR > 2
        ,CASE_VARIANT
#endif
#if CASE_MINOR >= 7 || CASE_MAJOR > 2
        ,CASE_OPTIONS
#endif
#if CASE_MINOR >= 12 || CASE_MAJOR > 2
        ,sizeof(ErlNifResourceTypeInit)
#endif
#if CASE_MINOR >= 14 || CASE_MAJOR > 2
        ,CASE_MINIMUM_ERTS
#endif
    };
    ERL_NIF_INIT_BODY;
    return (ErlNifEntry*)&entry651;
}
