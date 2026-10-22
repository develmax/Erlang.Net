using System.Text.Json;

namespace Erlang.Tests;

internal static class ProplistFamilyTestData
{
    internal static readonly ProplistFamilyTestCase[] Cases = JsonSerializer.Deserialize<ProplistFamilyTestCase[]>(Read(ProplistFamilyTestNames.CasesResource), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    static ProplistFamilyTestData()
    {
        if (Cases.Length != ProplistFamilyTestNames.ExpectedCases
            || Cases.Select(row => row.Name).Distinct(StringComparer.Ordinal).Count() != Cases.Length
            || Cases.Select(row => (row.Function, row.Arity)).Distinct().Count() != ProplistFamilyTestNames.ExpectedMfas)
        {
            throw new InvalidOperationException(ProplistFamilyTestNames.Failure);
        }
    }

    internal static string Read(string name)
    {
        using var stream = typeof(ProplistFamilyTestData).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(ProplistFamilyTestNames.MissingResource + name);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
