#include "erl_nif.h"

static ERL_NIF_TERM echo(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)env;
    (void)argc;
    return argv[0];
}

static ErlNifFunc functions[] = { {"echo", 1, echo, 0} };
ERL_NIF_INIT(raw_unix_filename_contract, functions, NULL, NULL, NULL, NULL)
