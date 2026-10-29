#include "erl_nif.h"
#include "entry_fixture_names.h"

static ERL_NIF_TERM identity(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    if (argc != 1)
        return enif_make_badarg(env);
    return argv[0];
}

static ERL_NIF_TERM marker(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc;
    (void)argv;
    return enif_make_int(env, ENTRY_ALPHA_MARKER);
}

static ERL_NIF_TERM greeting(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc;
    (void)argv;
    return enif_make_string(env, ENTRY_ALPHA_GREETING, ERL_NIF_LATIN1);
}

static ErlNifFunc functions[] = {
    {ENTRY_IDENTITY_NAME, 1, identity, 0},
    {ENTRY_MARKER_NAME, 0, marker, 0},
    {ENTRY_GREETING_NAME, 0, greeting, 0}
#ifdef ENTRY_DUPLICATE
    , {ENTRY_IDENTITY_NAME, 1, identity, 0}
#endif
};

ERL_NIF_INIT(native_entry_alpha, functions, NULL, NULL, NULL, NULL)
