namespace Erlang.Tests;

internal static class BeamEntryTupleCarrierDiagnostics
{
    internal const string WrongConstructor = "Entry tuple aliases must preserve reordered constructors and reuse only the matching value.";

    internal static string DisabledOption(string flag) => $"Entry tuple aliases must respect the {flag} build option.";

    internal static string MissingFunction(string name) => $"The entry tuple fixture is missing function {name}.";
}
