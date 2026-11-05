namespace Erlang.Tests;

internal static class StringPatternAllocationNames
{
    internal const string Test = "runtime/string/pattern-search/reuses-immutable-binary-buffer";
    internal const string Module = "string";
    internal const string Split = "split";
    internal const string Find = "find";
    internal const string All = "all";
    internal const string Trailing = "trailing";
    internal const string Warmup = "a,b,";
    internal const string Failure = "Repeated string search result or allocation budget differs";
    internal const string PublicationFailure = "Cannot publish pinned string search dependencies";
    internal const string Output = "artifacts/string-pattern-allocation/results.json";
    internal const int InputBytes = 512 * 1024;
    internal const int SeparatorPeriod = 1024;
    internal const byte Content = (byte)'b';
    internal const byte Separator = (byte)',';
    internal const long AllocationBudget = 2L * 1024 * 1024 * 1024;
    internal static readonly string[] Modes = [All, Trailing, Find];
    internal static readonly (string Module, string Resource, string Sha256)[] Images =
    [
        ("unicode", "Erlang.Tests.StandardBootOriginal412.unicode.beam", "2495812294976A34AE904E7709B75348F3DDDF3B950413199867A78E093A4379"),
        ("unicode_util", "Erlang.Tests.StandardBootOriginal412.unicode_util.beam", "5CDC886E30216B5DE33B6F4BDC1DEA006EAA3392EC4A83EC90CD7B946B5759FF"),
        ("erts_internal", "Erlang.Tests.StringPatternAllocation.erts_internal.beam", "6740F088697BCF6285227B95D3B91ECFF38AE89ADF9A1B6976413CEC2AF7B806")
    ];
}
