#include "erl_nif.h"

#define LARGE526 17
static ERL_NIF_TERM observe526(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM p, tuples, lists, nested, alias, map, map_tuple, map_list, key, found;
    unsigned char *bytes;
    if (argc!=1) return enif_make_badarg(env);
    p=argv[0];
    /* Every constructor below is a GENUINE variadic C API call. */
    tuples=enif_make_tuple(env,5,
        enif_make_tuple(env,0),
        enif_make_tuple(env,1,p),
        enif_make_tuple(env,2,p,p),
        enif_make_tuple(env,9,p,p,p,p,p,p,p,p,p),
        enif_make_tuple(env,LARGE526,p,p,p,p,p,p,p,p,p,p,p,p,p,p,p,p,p));
    lists=enif_make_tuple(env,5,
        enif_make_list(env,0),
        enif_make_list(env,1,p),
        enif_make_list(env,2,p,p),
        enif_make_list(env,9,p,p,p,p,p,p,p,p,p),
        enif_make_list(env,LARGE526,p,p,p,p,p,p,p,p,p,p,p,p,p,p,p,p,p));
    nested=enif_make_tuple(env,2,enif_make_list(env,2,tuples,lists),
        enif_make_tuple(env,2,enif_make_list(env,1,p),enif_make_tuple(env,1,p)));
    alias=enif_make_list(env,2,nested,nested);
    bytes=enif_make_new_binary(env,1,&key);
    if (!bytes) return enif_make_badarg(env);
    bytes[0]='a';
    map=enif_make_new_map(env);
    if (!enif_make_map_put(env,map,key,p,&map)) return enif_make_badarg(env);
    map_tuple=enif_make_tuple(env,2,map,map);
    map_list=enif_make_list(env,2,map,map);
    /* Preserve deferred/pending map children, not only static scalar handles. */
    bytes[0]='b';
    if (!enif_get_map_value(env,map,key,&found)) return enif_make_badarg(env);
    return enif_make_tuple(env,9,enif_make_int(env,enif_thread_type()),tuples,lists,nested,alias,
        map_tuple,map_list,map,enif_make_int(env,enif_is_identical(found,p)));
}
static ErlNifFunc functions526[]={
    {"normal",1,observe526,0},
    {"cpu",1,observe526,ERL_NIF_DIRTY_JOB_CPU_BOUND},
    {"io",1,observe526,ERL_NIF_DIRTY_JOB_IO_BOUND}
};
ERL_NIF_INIT(variadic_family526,functions526,NULL,NULL,NULL,NULL)
