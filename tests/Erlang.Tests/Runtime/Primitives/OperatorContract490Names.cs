namespace Erlang.Tests;

internal static class OperatorContract490Names
{
    internal const string TestPrefix = "runtime/bif-contract490/";
    internal const string Prefix = "Erlang.Tests.OperatorContract490.";
    internal const string Failure = "Operator contract resource is missing or differs";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "3DE3010E9A0888158523F91E2301A3BEBF16C954B01ADF60255C8BE5AE88AEF9";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string Separator = ".";
    internal static readonly (string Group, string Subject, int Values, int Profiles)[] Groups =
    [
        ("boolean_not", "operator_contract490_boolean_not", 38, 190),
        ("boolean_and", "operator_contract490_boolean_and", 151, 755),
        ("boolean_or", "operator_contract490_boolean_or", 151, 755),
        ("boolean_xor", "operator_contract490_boolean_xor", 151, 755),
        ("unary_plus", "operator_contract490_unary_plus", 38, 190),
        ("unary_minus", "operator_contract490_unary_minus", 38, 190),
        ("integer_complement", "operator_contract490_integer_complement", 38, 190)
    ];
}
