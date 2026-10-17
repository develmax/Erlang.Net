#include "erl_nif.h"
#include "entry_fixture_names.h"

#ifndef ENTRY_WITH_LOAD
#define ENTRY_WITH_LOAD 1
#endif
#ifndef ENTRY_WITH_UNLOAD
#define ENTRY_WITH_UNLOAD 1
#endif

#if ENTRY_WITH_LOAD
static int marker_value;
static int load_entry(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    if (!enif_get_int(env, info, &marker_value))
        return 1;
    *private_data = &marker_value;
    return 0;
}
#define ENTRY_LOAD load_entry
#else
#define ENTRY_LOAD NULL
#endif

#if ENTRY_WITH_UNLOAD
static void unload_entry(ErlNifEnv *env, void *private_data)
{
    (void)env;
    (void)private_data;
}
#define ENTRY_UNLOAD unload_entry
#else
#define ENTRY_UNLOAD NULL
#endif

static ERL_NIF_TERM identity(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    if (argc != 1)
        return enif_make_badarg(env);
    return argv[0];
}

static ERL_NIF_TERM marker(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int *value = enif_priv_data(env);
    (void)argc;
    (void)argv;
    return enif_make_int(env, value ? *value : ENTRY_BETA_DEFAULT_MARKER);
}

static ERL_NIF_TERM latin1_name(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argc;
    (void)argv;
    return enif_make_int(env, ENTRY_LATIN1_RESULT);
}

static ErlNifFunc functions[] = {
    {ENTRY_IDENTITY_NAME, 1, identity, 0},
    {ENTRY_MARKER_NAME, 0, marker, 0},
    {ENTRY_LATIN1_NAME, 0, latin1_name, 0}
#ifdef ENTRY_UNDECLARED
    , {ENTRY_ORDINARY_NAME, 0, marker, 0}
#endif
#ifdef ENTRY_MISSING
    , {ENTRY_MISSING_NAME, 0, marker, 0}
#endif
};

#ifndef ENTRY_LATIN1_MODULE
ERL_NIF_INIT(native_entry_beta, functions, ENTRY_LOAD, NULL, NULL, ENTRY_UNLOAD)
#else
ERL_NIF_INIT_PROLOGUE
ERL_NIF_INIT_GLOB
ERL_NIF_INIT_DECL(native_entry_latin1);
ERL_NIF_INIT_DECL(native_entry_latin1)
{
    static ErlNifEntry entry = {
        ERL_NIF_MAJOR_VERSION, ERL_NIF_MINOR_VERSION, "native_entry_b\351ta",
        sizeof(functions) / sizeof(*functions), functions,
        ENTRY_LOAD, NULL, NULL, ENTRY_UNLOAD,
        ERL_NIF_VM_VARIANT, 1, sizeof(ErlNifResourceTypeInit), ERL_NIF_MIN_ERTS_VERSION
    };
    ERL_NIF_INIT_BODY;
    return &entry;
}
ERL_NIF_INIT_EPILOGUE
#endif
