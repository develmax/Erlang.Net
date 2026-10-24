using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class ProplistInternalHashHostChecks
{
    internal static async Task NativeVectors()
    {
        Type engine = typeof(ProcessRuntime).Assembly.GetType(ProplistHashHostNames.HashType)
            ?? throw new InvalidDataException(ProplistHashHostNames.MissingEngine);
        Type provider = typeof(ProcessRuntime).Assembly.GetType(ProplistHashHostNames.ProviderType)
            ?? throw new InvalidDataException(ProplistHashHostNames.MissingEngine);
        MethodInfo hash = engine.GetMethod(ProplistHashHostNames.HashMethod, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidDataException(ProplistHashHostNames.MissingEngine);
        object identities = Activator.CreateInstance(
            provider,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [null],
            CultureInfo.InvariantCulture
        ) ?? throw new InvalidDataException(ProplistHashHostNames.MissingEngine);
        var rows = ProplistHashHostResources.Rows(ProplistHashHostResources.Read(ProplistHashHostNames.HashResource));
        if (rows.Count != ProplistHashHostNames.NativeCount || !rows.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(ProplistHashHostNames.HashCases))
            throw new InvalidDataException(ProplistHashHostNames.InputFailure);
        var failures = new List<Exception>();
        int checkedRows = 0;
        foreach (var row in rows)
        {
            // Atom index is runtime state; its original numeric hash is not a native-equality assertion.
            if (row.Key == ProplistHashHostNames.AtomCase)
                continue;
            try
            {
                if (row.Value is not TupleTerm { Items.Count: 2 } observed
                    || !observed.Items[0].Equals(Term.A(ProplistHashHostNames.Ok))
                    || observed.Items[1] is not TupleTerm { Items.Count: 2 } pair
                    || pair.Items[1] is not Integer expected || expected.Value < 0 || expected.Value > ulong.MaxValue)
                    throw new InvalidDataException(ProplistHashHostNames.InputFailure);
                checkedRows++;
                if (hash.Invoke(null, [pair.Items[0], identities]) is not ulong actual || actual != (ulong)expected.Value)
                    throw new InvalidDataException(ProplistHashHostNames.HashMismatch + row.Key);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (checkedRows != ProplistHashHostNames.PrimitiveCount)
            failures.Add(new InvalidDataException(ProplistHashHostNames.InputFailure));
        if (failures.Count != 0)
            throw new AggregateException(failures);
        Term atomRow = rows[ProplistHashHostNames.AtomCase];
        object? ownHash = atomRow is TupleTerm { Items.Count: 2 } atomOutcome && atomOutcome.Items[1] is TupleTerm { Items.Count: 2 } atomPair
            ? hash.Invoke(null, [atomPair.Items[0], identities]) : null;
        Directory.CreateDirectory(Path.GetDirectoryName(ProplistHashHostNames.KnownHashReport)!);
        await File.WriteAllTextAsync(
            ProplistHashHostNames.KnownHashReport,
            JsonSerializer.Serialize(
                new
                {
                    Name = ProplistHashHostNames.AtomCase,
                    Pending = true,
                    PrimitiveNativeEqualChecks = checkedRows,
                    NativeOutcomeEtf = Convert.ToBase64String(ExternalTermFormat.Encode(atomRow)),
                    OwnAtomHash = ownHash,
                    CompleteContractClaimed = false
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }
}
