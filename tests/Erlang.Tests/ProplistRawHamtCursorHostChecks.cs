using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistRawHamtCursorHostChecks
{
    internal static async Task IntegerAndBinaryStates()
    {
        Term originalRows = ProplistHashHostResources.Read(ProplistHashHostNames.CursorResource);
        var expected = ProplistHashHostResources.Rows(originalRows);
        var completeRoster = ProplistHashHostNames.CursorCases.Concat(new[] { ProplistHashHostNames.TupleAtomCase, ProplistHashHostNames.AtomCursorCase }).ToHashSet(StringComparer.Ordinal);
        if (expected.Count != ProplistHashHostNames.CursorCount || !expected.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(completeRoster))
        {
            throw new InvalidDataException(ProplistHashHostNames.InputFailure);
        }
        using var compiled = GeneratedModuleCompiler.Compile(ProplistHashHostResources.Source(), ProplistHashHostNames.SourceFile);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term returned = await owner.InvokeAsync(context => Execution.InvokeAsync(
            compiled.Definition,
            ProplistHashHostNames.Run,
            context,
            []
        ).AsTask())
            .WaitAsync(ProplistHashHostNames.Deadline);
        var actual = ProplistHashHostResources.Rows(returned);
        if (actual.Count != ProplistHashHostNames.CursorCount || !actual.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(completeRoster))
        {
            throw new InvalidDataException(ProplistHashHostNames.InputFailure);
        }
        var failures = new List<Exception>();
        int compared = 0;
        foreach (string name in ProplistHashHostNames.CursorCases)
        {
            compared++;
            if (!expected[name].Equals(actual[name]))
            {
                failures.Add(new InvalidDataException(ProplistHashHostNames.CursorMismatch + name));
            }
        }
        if (compared != ProplistHashHostNames.SupportedCursorCount)
        {
            failures.Add(new InvalidDataException(ProplistHashHostNames.InputFailure));
        }
        var supportedNames = ProplistHashHostNames.CursorCases.ToHashSet(StringComparer.Ordinal);
        Term originalSubset = Cons.From(Cons.Items(originalRows).Where(row => row is TupleTerm { Items.Count: 2 } pair
            && pair.Items[0] is Atom name && supportedNames.Contains(name.Name)));
        Term actualSubset = Cons.From(Cons.Items(returned).Where(row => row is TupleTerm { Items.Count: 2 } pair
            && pair.Items[0] is Atom name && supportedNames.Contains(name.Name)));
        if (!originalSubset.Equals(actualSubset))
        {
            failures.Add(new InvalidDataException(ProplistHashHostNames.CursorSequenceMismatch));
        }
        var known = new[] { ProplistHashHostNames.TupleAtomCase, ProplistHashHostNames.AtomCursorCase }.Select(name => new
        {
            Name = name,
            Pending = true,
            Equal = expected[name].Equals(actual[name]),
            OriginalOutcomeEtf = Convert.ToBase64String(ExternalTermFormat.Encode(expected[name])),
            ActualOutcomeEtf = Convert.ToBase64String(ExternalTermFormat.Encode(actual[name]))
        }).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(ProplistHashHostNames.KnownCursorReport)!);
        await File.WriteAllTextAsync(
            ProplistHashHostNames.KnownCursorReport,
            JsonSerializer.Serialize(
                new
                {
                    SupportedChecks = compared,
                    OriginalSupportedRowsEtf = Convert.ToBase64String(ExternalTermFormat.Encode(originalSubset)),
                    ActualSupportedRowsEtf = Convert.ToBase64String(ExternalTermFormat.Encode(actualSubset)),
                    CompleteContractClaimed = false,
                    NativeAtomTopologyPending = true,
                    KnownDomains = known
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (failures.Count != 0)
        {
            throw new AggregateException(failures);
        }
    }
}
