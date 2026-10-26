namespace Erlang.Tests;

internal static class ArithmeticContract491Names
{
    internal const string TestPrefix = "runtime/bif-contract491/";
    internal const string Prefix = "Erlang.Tests.ArithmeticContract491.";
    internal const string Failure = "Arithmetic contract resource is missing or differs";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "AA8B3569062C51DC112772E7FC36A29B6B23454C418E093C5EECEEFCE649AB8C";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string Separator = ".";
    internal static readonly (string Group, string Subject, int Values, int Profiles)[] Groups =
    [
        ("add_integers", "arithmetic_contract491_add_integers", 169, 845),
        ("add_float_left", "arithmetic_contract491_add_float_left", 168, 840),
        ("add_float_right", "arithmetic_contract491_add_float_right", 104, 520),
        ("add_invalid", "arithmetic_contract491_add_invalid", 51, 255),
        ("add_identity", "arithmetic_contract491_add_identity", 16, 80),
        ("subtract_integers", "arithmetic_contract491_subtract_integers", 169, 845),
        ("subtract_float_left", "arithmetic_contract491_subtract_float_left", 168, 840),
        ("subtract_float_right", "arithmetic_contract491_subtract_float_right", 104, 520),
        ("subtract_invalid", "arithmetic_contract491_subtract_invalid", 51, 255),
        ("subtract_identity", "arithmetic_contract491_subtract_identity", 16, 80),
        ("multiply_integers", "arithmetic_contract491_multiply_integers", 169, 845),
        ("multiply_float_left", "arithmetic_contract491_multiply_float_left", 168, 840),
        ("multiply_float_right", "arithmetic_contract491_multiply_float_right", 104, 520),
        ("multiply_invalid", "arithmetic_contract491_multiply_invalid", 51, 255),
        ("multiply_identity", "arithmetic_contract491_multiply_identity", 16, 80),
        ("divide_integers", "arithmetic_contract491_divide_integers", 169, 845),
        ("divide_float_left", "arithmetic_contract491_divide_float_left", 168, 840),
        ("divide_float_right", "arithmetic_contract491_divide_float_right", 104, 520),
        ("divide_invalid", "arithmetic_contract491_divide_invalid", 51, 255),
        ("divide_identity", "arithmetic_contract491_divide_identity", 16, 80),
        ("integer_divide_integers", "arithmetic_contract491_integer_divide_integers", 169, 845),
        ("integer_divide_invalid", "arithmetic_contract491_integer_divide_invalid", 75, 375),
        ("integer_divide_identity", "arithmetic_contract491_integer_divide_identity", 16, 80),
        ("remainder_integers", "arithmetic_contract491_remainder_integers", 169, 845),
        ("remainder_invalid", "arithmetic_contract491_remainder_invalid", 75, 375),
        ("remainder_identity", "arithmetic_contract491_remainder_identity", 16, 80),
        ("integer_and_integers", "arithmetic_contract491_integer_and_integers", 169, 845),
        ("integer_and_invalid", "arithmetic_contract491_integer_and_invalid", 75, 375),
        ("integer_and_identity", "arithmetic_contract491_integer_and_identity", 16, 80),
        ("integer_or_integers", "arithmetic_contract491_integer_or_integers", 169, 845),
        ("integer_or_invalid", "arithmetic_contract491_integer_or_invalid", 75, 375),
        ("integer_or_identity", "arithmetic_contract491_integer_or_identity", 16, 80),
        ("integer_xor_integers", "arithmetic_contract491_integer_xor_integers", 169, 845),
        ("integer_xor_invalid", "arithmetic_contract491_integer_xor_invalid", 75, 375),
        ("integer_xor_identity", "arithmetic_contract491_integer_xor_identity", 16, 80)
    ];
}
