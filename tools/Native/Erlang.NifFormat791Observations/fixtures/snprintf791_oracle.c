#include "erl_nif.h"
#include <limits.h>
#include <stdint.h>
#include <string.h>

enum { SNAPSHOT_BYTES791 = 128, MAXIMUM_ROWS791 = 48, SENTINEL791 = 0xA5 };

static ERL_NIF_TERM capture791(ErlNifEnv *env, const char *name, unsigned size,
                               int returned, const unsigned char *buffer, int counter)
{
    ERL_NIF_TERM binary;
    unsigned char *bytes = enif_make_new_binary(env, SNAPSHOT_BYTES791, &binary);
    if (bytes == NULL) return enif_make_badarg(env);
    memcpy(bytes, buffer, SNAPSHOT_BYTES791);
    return enif_make_tuple5(env, enif_make_atom(env, name), enif_make_uint(env, size),
                            enif_make_int(env, returned), binary, enif_make_int(env, counter));
}

static ERL_NIF_TERM run791(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    unsigned char buffer[SNAPSHOT_BYTES791];
    ERL_NIF_TERM rows[MAXIMUM_ROWS791];
    ERL_NIF_TERM ok = enif_make_atom(env, "ok");
    ERL_NIF_TERM tuple = enif_make_tuple2(env, ok, enif_make_int(env, 42));
    ERL_NIF_TERM map = enif_make_new_map(env);
    unsigned count = 0;
    int counter;
    (void)argc;
    (void)argv;
    if (!enif_make_map_put(env, map, enif_make_int(env, 1), ok, &map)
        || !enif_make_map_put(env, map, enif_make_int(env, 2), enif_make_int(env, 42), &map))
        return enif_make_badarg(env);
#define ROW791(NAME, SIZE, ...) do { \
    int returned791; \
    counter = -1; \
    memset(buffer, SENTINEL791, sizeof(buffer)); \
    returned791 = enif_snprintf((char *)buffer, (SIZE), __VA_ARGS__); \
    rows[count++] = capture791(env, (NAME), (SIZE), returned791, buffer, counter); \
} while (0)
    ROW791("crypto_pair", 64, "%s:%s", "OpenSSL", "3.5.1");
    ROW791("size_zero_untouched", 0, "%s", "abcdef");
    ROW791("size_one", 1, "%s", "abcdef");
    ROW791("size_exact", 7, "%s", "abcdef");
    ROW791("truncated_required_count", 4, "%s", "abcdef");
    ROW791("utf8_raw", 16, "%s", "A\xC3\xA9" "B");
    ROW791("utf8_partial_byte", 3, "%s", "A\xC3\xA9" "B");
    ROW791("string_byte_precision", 16, "%.1s", "\xC3\xA9");
    ROW791("string_byte_width", 16, "%8s", "A\xC3\xA9" "B");
    ROW791("string_left", 16, "%-8.3s", "abcdef");
    ROW791("bare_precision_source_rule", 16, "%.s", "abcdef");
    ROW791("negative_star_width_source_rule", 16, "%*s", -8, "abc");
    ROW791("positive_star_precision", 16, "%*.*s", 8, 3, "abcdef");
    ROW791("negative_star_precision", 16, "%.*s", -1, "abcdef");
    ROW791("signed_flags", 64, "%+08d|% d|%-8d", -42, 42, -7);
    ROW791("signed_zero_precision", 64, "%+.0d|%08.4d", 0, -42);
    ROW791("signed_int_min", 64, "%d", INT_MIN);
    ROW791("signed_long_min", 64, "%ld", LONG_MIN);
    ROW791("signed_long_long_min", 64, "%lld", LLONG_MIN);
    ROW791("unsigned_maxima", 96, "%u|%lu|%llu", UINT_MAX, ULONG_MAX, ULLONG_MAX);
    ROW791("integer_narrow_lengths", 64, "%hhd|%hd|%hhu|%hu", -128, -32768, 255, 65535);
    ROW791("integer_source_bit_lengths", 96, "%b8d|%b16u|%b32u|%b64u", -128, 65535, UINT_MAX, (uint64_t)ULLONG_MAX);
    ROW791("alternate_bases", 64, "%#08x|%#X|%#.0x|%#.0o", 42u, 42u, 0u, 0u);
    ROW791("large_width_full_count", 8, "%500u", 7u);
    ROW791("null_pointer_source_rule", 64, "%p", (void *)0);
    ROW791("constant_pointer", 64, "%p", (void *)(uintptr_t)0x1234);
    ROW791("character_zero_precision", 16, "%.0c|%3c|%-3c", 'A', 'B', 'C');
    ROW791("embedded_nul_character", 16, "A%cB", 0);
    ROW791("percent", 16, "x%%y");
    ROW791("native_count_on_truncation", 4, "abcdef%nG", &counter);
    ROW791("term_tuple", 64, "%T", tuple);
    ROW791("term_map", 64, "%T", map);
    ROW791("term_quoted_atom", 64, "%T", enif_make_atom(env, "space atom"));
    ROW791("term_latin1_atom_utf8_output", 64, "%T", enif_make_atom(env, "caf\xE9"));
    ROW791("term_printable_list", 64, "%T", enif_make_string(env, "abc", ERL_NIF_LATIN1));
    ROW791("term_precision_zero", 64, "%.0T", tuple);
    ROW791("term_precision_one", 64, "%.1T", tuple);
    ROW791("term_precision_two", 64, "%.2T", tuple);
    ROW791("term_width", 64, "%12T|%-12T", tuple, tuple);
    ROW791("term_truncation_full_count", 4, "%T", tuple);
    ROW791("source_long_double_error_clears", 64, "before%Lf", (long double)1.5);
#undef ROW791
    return enif_make_list_from_array(env, rows, count);
}

static ErlNifFunc functions791[] = {{"run", 0, run791, 0}};
ERL_NIF_INIT(snprintf791_oracle, functions791, NULL, NULL, NULL, NULL)
