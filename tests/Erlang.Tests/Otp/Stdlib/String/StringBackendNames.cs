namespace Erlang.Tests;

internal static class StringBackendNames
{
    internal const string ManagedTest = "runtime/string/managed-selection-and-original-contracts";
    internal const string BeamTest = "runtime/string/original-selection-and-original-contracts";
    internal const string ManagedJaroTest = "runtime/string/managed-jaro-indexed-resource-contract";
    internal const string BeamJaroTest = "runtime/string/original-jaro-indexed-resource-contract";
    internal const string JaroFixture = "jaro-regression.etf";
    internal const int CasesPerActorCommand = 16;
    internal const string RefusalTest = "runtime/string/explicit-dependencies-and-no-fallback";
    internal const string Failure = "String backend contract differs from pinned OTP";
    internal const string Prefix = "Erlang.Tests.StringBackend.";
    internal const string Observer = "string_port_cases";
    internal static readonly string[] Dependencies = ["erlang", "lists", "unicode", "unicode_util"];
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["string"] = "37C66C6A5DF38AE239CA1EEDEDBD2CE2FBC4B8B4F3EB7605E115D0AB73FDA697",
        ["unicode"] = "2495812294976A34AE904E7709B75348F3DDDF3B950413199867A78E093A4379",
        ["unicode_util"] = "5CDC886E30216B5DE33B6F4BDC1DEA006EAA3392EC4A83EC90CD7B946B5759FF"
    };
}
