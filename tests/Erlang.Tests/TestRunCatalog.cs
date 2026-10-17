using System.Collections.ObjectModel;

namespace Erlang.Tests;

internal sealed class TestRunCatalog
{
    private readonly IReadOnlyDictionary<string, string> blocksByName;

    internal IReadOnlyList<string> Names { get; }

    private TestRunCatalog(string[] names, Dictionary<string, string> blocks)
    {
        Names = Array.AsReadOnly(names);
        blocksByName = new ReadOnlyDictionary<string, string>(blocks);
    }

    internal static TestRunCatalog Create(IEnumerable<string> testNames)
    {
        string[] names = testNames.ToArray();
        if (names.Length == 0 || names.Any(string.IsNullOrWhiteSpace)
            || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new ArgumentException(TestRunNames.InvalidCatalog);
        var blocks = names.ToDictionary(name => name, Classify, StringComparer.Ordinal);

        return new TestRunCatalog(names, blocks);
    }

    internal string BlockFor(string name) => blocksByName[name];

    internal bool Contains(string name) => blocksByName.ContainsKey(name);

    private static string Classify(string name)
    {
        string value = name.ToLowerInvariant();
        bool compiled = value.StartsWith(TestRunNames.CompiledPrefix, StringComparison.Ordinal);
        if (compiled)
            value = value[TestRunNames.CompiledPrefix.Length..];
        if (Matches(value, TestRunNames.NativeMarkers))
            return TestRunNames.Native;
        if (Matches(value, TestRunNames.PortsMarkers))
            return TestRunNames.PortsDistribution;
        if (value.StartsWith(TestRunNames.CompilerPrefix, StringComparison.Ordinal))
            return TestRunNames.Compiler;
        if (Matches(value, TestRunNames.OtpMarkers))
            return TestRunNames.Otp;
        if (Matches(value, TestRunNames.ProcessMarkers))
            return TestRunNames.Processes;
        if (Matches(value, TestRunNames.TermMarkers))
            return TestRunNames.Terms;
        if (compiled || Matches(value, TestRunNames.CompilerMarkers))
            return TestRunNames.Compiler;

        return TestRunNames.Tooling;
    }

    private static bool Matches(string value, IEnumerable<string> markers)
        => markers.Any(marker => value.Contains(marker, StringComparison.Ordinal));
}
