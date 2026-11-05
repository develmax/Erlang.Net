namespace Erlang.Tests;

internal static class StringSeparatorAllocationNames
{
    internal const string Test = "runtime/string/separator-search/reuses-immutable-binary-buffer";
    internal const string Module = "string";
    internal const string Trim = "trim";
    internal const string Take = "take";
    internal const string Lexemes = "lexemes";
    internal const string Trailing = "trailing";
    internal const string False = "false";
    internal const string Failure = "Repeated separator search result or allocation budget differs";
    internal const string Output = "artifacts/string-separator-allocation/results.json";
    internal const int InputBytes = 64 * 1024;
    internal const int SeparatorPeriod = 128;
    internal const byte Content = (byte)'a';
    internal const byte Separator = (byte)' ';
    internal const long TrimAllocationBudget = 315_000_000;
    internal const long TakeAllocationBudget = 310_000_000;
    internal const long LexemesAllocationBudget = 355_000_000;
    internal static readonly string[] Functions = [Trim, Take, Lexemes];
}
