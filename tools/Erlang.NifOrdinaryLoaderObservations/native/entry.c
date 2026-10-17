#include "erl_nif.h"
extern int ordinary_dependency_value(void);
int path_value(void) { return ordinary_dependency_value(); }
static ERL_NIF_TERM entryprobe(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_int(env, path_value());
}
static ErlNifFunc functions[] = {{"entryprobe", 0, entryprobe, 0}};
ERL_NIF_INIT(ordinary_loader_probe, functions, NULL, NULL, NULL, NULL)
