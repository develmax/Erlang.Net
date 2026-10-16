#include "erl_nif.h"
#include "reallocation_probe_names.h"

/* Additional trusted probe only. Never replace the legacy C7B6A fixture.
 * Before calling: original collector MUST verify enabled driver_alloc using
 * pinned alloc_util (ALLOC_ZERO_EQ_NULL=0); SDK uses positive backing >=1.
 * Under these two reviewed profiles, NULL from realloc leaves the current
 * pointer owned even for requested zero. Disabled/libc-zero profiles are denied.
 */
static void write_payload(volatile unsigned char *pointer, size_t count, unsigned pattern)
{
    size_t index;
    for (index = 0; index < count; index++)
        pointer[index] = (unsigned char)((pattern + index) % REALLOCATION_PATTERN_BYTE_MODULUS);
}

static int verify_payload(const volatile unsigned char *pointer, size_t count, unsigned pattern)
{
    size_t index;
    for (index = 0; index < count; index++)
        if (pointer[index] != (unsigned char)((pattern + index) % REALLOCATION_PATTERN_BYTE_MODULUS))
            return 0;
    return 1;
}

static int run_probe(ErlNifEnv *env, int selected)
{
    unsigned char *pointer = NULL;
    unsigned char *replacement;
    int mask = REALLOCATION_RESULT_NO_BITS;
    size_t initial_size = selected == REALLOCATION_CASE_NULL_ZERO
        ? REALLOCATION_SIZE_ZERO : REALLOCATION_SIZE_INITIAL;

    if (enif_has_pending_exception(env, NULL))
        return mask;
    pointer = enif_realloc(NULL, initial_size);
    if (pointer == NULL)
        goto finish;
    mask |= REALLOCATION_BIT_INITIAL_NON_NULL;
    if (selected == REALLOCATION_CASE_NULL_ZERO)
    {
        mask |= REALLOCATION_BIT_ZERO_NON_NULL;
        goto finish;
    }
    write_payload(pointer, REALLOCATION_SIZE_INITIAL, REALLOCATION_PATTERN_INITIAL);
    if (!verify_payload(pointer, REALLOCATION_SIZE_INITIAL, REALLOCATION_PATTERN_INITIAL))
        goto finish;
    mask |= REALLOCATION_BIT_INITIAL_PAYLOAD_VERIFIED;
    if (selected == REALLOCATION_CASE_NULL_POSITIVE)
        goto finish;

    replacement = enif_realloc(pointer, REALLOCATION_SIZE_GROWN);
    if (replacement == NULL)
        goto finish;
    pointer = replacement;
    mask |= REALLOCATION_BIT_GROW_NON_NULL;
    if (!verify_payload(pointer, REALLOCATION_SIZE_INITIAL, REALLOCATION_PATTERN_INITIAL))
        goto finish;
    mask |= REALLOCATION_BIT_GROW_PREFIX_VERIFIED;
    write_payload(pointer, REALLOCATION_SIZE_GROWN, REALLOCATION_PATTERN_GROWN);

    replacement = enif_realloc(pointer, REALLOCATION_SIZE_SHRUNK);
    if (replacement == NULL)
        goto finish;
    pointer = replacement;
    mask |= REALLOCATION_BIT_SHRINK_NON_NULL;
    if (!verify_payload(pointer, REALLOCATION_SIZE_SHRUNK, REALLOCATION_PATTERN_GROWN))
        goto finish;
    mask |= REALLOCATION_BIT_SHRINK_PREFIX_VERIFIED;

    replacement = enif_realloc(pointer, REALLOCATION_SIZE_ZERO);
    if (replacement == NULL)
        goto finish;
    pointer = replacement;
    mask |= REALLOCATION_BIT_ZERO_NON_NULL;
    /* Zero requested bytes: no dereference and no preservation assertion. */
    replacement = enif_realloc(pointer, REALLOCATION_SIZE_AFTER_ZERO);
    if (replacement == NULL)
        goto finish;
    pointer = replacement;
    mask |= REALLOCATION_BIT_AFTER_ZERO_NON_NULL;
    write_payload(pointer, REALLOCATION_SIZE_AFTER_ZERO, REALLOCATION_PATTERN_AFTER_ZERO);
    if (!verify_payload(pointer, REALLOCATION_SIZE_AFTER_ZERO, REALLOCATION_PATTERN_AFTER_ZERO))
        goto finish;
    mask |= REALLOCATION_BIT_AFTER_ZERO_NEW_PAYLOAD_VERIFIED;

finish:
    if (pointer != NULL)
    {
        enif_free(pointer);
        mask |= REALLOCATION_BIT_CURRENT_POINTER_FREE_RETURNED;
    }
    if (!enif_has_pending_exception(env, NULL))
        mask |= REALLOCATION_BIT_NO_PENDING_EXCEPTION;
    return mask;
}

static int load_probe(ErlNifEnv *env, void **private_data, ERL_NIF_TERM info)
{
    int marker;
    if (!enif_get_int(env, info, &marker) || marker != REALLOCATION_LOAD_MARKER)
        return REALLOCATION_LOAD_VETO;
    *private_data = NULL;
    return REALLOCATION_LOAD_ACCEPT;
}

static void unload_probe(ErlNifEnv *env, void *private_data)
{
    (void)env;
    (void)private_data;
}

static ERL_NIF_TERM increment(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    int selected;
    if (argc != REALLOCATION_INCREMENT_ARITY || !enif_get_int(env, argv[0], &selected) ||
        (selected != REALLOCATION_CASE_NULL_POSITIVE && selected != REALLOCATION_CASE_NULL_ZERO &&
         selected != REALLOCATION_CASE_RESIZE_SEQUENCE))
        return enif_make_badarg(env);
    return enif_make_int(env, run_probe(env, selected));
}

static ERL_NIF_TERM greeting(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    (void)argv;
    if (argc != REALLOCATION_GREETING_ARITY)
        return enif_make_badarg(env);
    return enif_make_string(env, REALLOCATION_PROBE_GREETING, ERL_NIF_LATIN1);
}

static ErlNifFunc functions[] = {
    {REALLOCATION_INCREMENT_NAME, REALLOCATION_INCREMENT_ARITY, increment, REALLOCATION_FUNCTION_FLAGS},
    {REALLOCATION_GREETING_NAME, REALLOCATION_GREETING_ARITY, greeting, REALLOCATION_FUNCTION_FLAGS}
};

ERL_NIF_INIT(memory_allocator_nif_fixture, functions, load_probe, NULL, NULL, unload_probe)
