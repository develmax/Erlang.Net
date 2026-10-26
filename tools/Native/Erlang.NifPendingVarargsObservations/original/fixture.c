#include "erl_nif.h"

#define SPILL_COUNT 17
#define LOAD_MARKER 42
#define FIRST_REASON 101
#define LAST_REASON 202
#define ORDINARY_VALUE 303
#define UNTOUCHED_REASON 777
#define UNUSED_ARGS() do { (void)argc; (void)argv; } while (0)

/* Test infrastructure: construction values are deliberate case inputs, not runtime algorithms. */
static int load_fixture(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    int marker;
    (void)private_data;

    return enif_get_int(env, info, &marker) && marker == LOAD_MARKER ? 0 : 1;
}

static ERL_NIF_TERM tuple_empty(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();

    return enif_make_tuple(env, 0);
}

static ERL_NIF_TERM tuple_single(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();

    return enif_make_tuple(env, 1, enif_make_int(env, LOAD_MARKER));
}

static void spill_values(ErlNifEnv *env, ERL_NIF_TERM *values)
{
    int index;

    for (index = 0; index < SPILL_COUNT; index++)
        values[index] = enif_make_int(env, index);
}

static ERL_NIF_TERM tuple_spill(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM v[SPILL_COUNT];
    UNUSED_ARGS();
    spill_values(env, v);

    return enif_make_tuple(env, SPILL_COUNT, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7],
                           v[8], v[9], v[10], v[11], v[12], v[13], v[14], v[15], v[16]);
}

static ERL_NIF_TERM list_empty(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();

    return enif_make_list(env, 0);
}

static ERL_NIF_TERM list_single(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();

    return enif_make_list(env, 1, enif_make_int(env, LOAD_MARKER));
}

static ERL_NIF_TERM list_spill(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM v[SPILL_COUNT];
    UNUSED_ARGS();
    spill_values(env, v);

    return enif_make_list(env, SPILL_COUNT, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7],
                          v[8], v[9], v[10], v[11], v[12], v[13], v[14], v[15], v[16]);
}

static ERL_NIF_TERM pending_none(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM reason = enif_make_int(env, UNTOUCHED_REASON);
    int flag, null_flag, regular;
    UNUSED_ARGS();
    null_flag = enif_has_pending_exception(env, NULL);
    flag = enif_has_pending_exception(env, &reason);
    regular = enif_is_exception(env, enif_make_int(env, ORDINARY_VALUE));

    return enif_make_tuple4(env, enif_make_int(env, null_flag), enif_make_int(env, flag),
                            reason, enif_make_int(env, regular));
}

static ERL_NIF_TERM pending_raise_twice(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();
    (void)enif_raise_exception(env, enif_make_int(env, FIRST_REASON));
    (void)enif_raise_exception(env, enif_make_int(env, LAST_REASON));

    return enif_make_int(env, ORDINARY_VALUE);
}

static ERL_NIF_TERM pending_badarg_ignored(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();
    (void)enif_make_badarg(env);

    return enif_make_int(env, ORDINARY_VALUE);
}

static ERL_NIF_TERM pending_detail(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM reason = enif_make_int(env, UNTOUCHED_REASON);
    ERL_NIF_TERM sentinel, detail;
    int flag, null_flag, exception_flag, regular_flag;
    UNUSED_ARGS();
    (void)enif_raise_exception(env, enif_make_int(env, FIRST_REASON));
    sentinel = enif_raise_exception(env, enif_make_int(env, LAST_REASON));
    flag = enif_has_pending_exception(env, &reason);
    null_flag = enif_has_pending_exception(env, NULL);
    exception_flag = enif_is_exception(env, sentinel);
    regular_flag = enif_is_exception(env, enif_make_int(env, ORDINARY_VALUE));
    detail = enif_make_tuple5(env, enif_make_int(env, flag), reason, enif_make_int(env, null_flag),
                             enif_make_int(env, exception_flag), enif_make_int(env, regular_flag));
    /* Preserve raw observations in a final exception reason; do not encode pass/fail in C. */
    (void)enif_raise_exception(env, detail);

    return enif_make_int(env, ORDINARY_VALUE);
}

static ERL_NIF_TERM pending_badarg_then_raise(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();
    (void)enif_make_badarg(env);
    (void)enif_raise_exception(env, enif_make_int(env, LAST_REASON));

    return enif_make_int(env, ORDINARY_VALUE);
}

static ERL_NIF_TERM pending_raise_then_badarg(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    UNUSED_ARGS();
    (void)enif_raise_exception(env, enif_make_int(env, FIRST_REASON));
    (void)enif_make_badarg(env);

    return enif_make_int(env, ORDINARY_VALUE);
}

static ErlNifFunc functions[] = {
    {"tuple_empty", 0, tuple_empty, 0},
    {"tuple_single", 0, tuple_single, 0},
    {"tuple_spill", 0, tuple_spill, 0},
    {"list_empty", 0, list_empty, 0},
    {"list_single", 0, list_single, 0},
    {"list_spill", 0, list_spill, 0},
    {"pending_none", 0, pending_none, 0},
    {"pending_raise_twice", 0, pending_raise_twice, 0},
    {"pending_badarg_ignored", 0, pending_badarg_ignored, 0},
    {"pending_detail", 0, pending_detail, 0},
    {"pending_badarg_then_raise", 0, pending_badarg_then_raise, 0},
    {"pending_raise_then_badarg", 0, pending_raise_then_badarg, 0}
};

ERL_NIF_INIT(nif_pending_varargs_fixture, functions, load_fixture, NULL, NULL, NULL)
