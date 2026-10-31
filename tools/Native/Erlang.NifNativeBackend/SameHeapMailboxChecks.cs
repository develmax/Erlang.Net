using System.Reflection;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

public static class SameHeapMailboxChecks
{
    public static async Task PhysicalSelfSend()
    {
        foreach (bool owned in new[] { false, true })
            foreach (string mode in new[] { SameHeapMailboxCaseNames.Operator, SameHeapMailboxCaseNames.Bif })
            {
                using var compiled = GeneratedModuleCompiler.Compile(SameHeapMailboxCaseNames.Source, SameHeapMailboxCaseNames.File);
                var runtime = NativeOwned516Profile.Create(owned);
                var observations = new Dictionary<int, NativeWritableObservation>();
                compiled.Definition.Register(runtime.Modules);
                runtime.Modules.Register(
                    SameHeapMailboxCaseNames.Probe,
                    SameHeapMailboxCaseNames.Checkpoint,
                    1,
                    (context, values) =>
                    {
                        observations.Add(checked((int)((Integer)values[0]).Value), NativeWritableTestAccess.Capture(context, false));

                        return ValueTask.FromResult<Term>(Term.A(SameHeapMailboxCaseNames.Ok));
                    }
                );
                try
                {
                    Term result = await CpuStatisticsAccess.Actor<Term>(
                        runtime,
                        async context =>
                        await runtime.Modules.Call(
                            context,
                            SameHeapMailboxCaseNames.Module,
                            SameHeapMailboxCaseNames.Run,
                            Term.A(mode)
                        )
                    );
                    Require(result is TupleTerm { Items.Count: 5 });
                    var tuple = (TupleTerm)result;
                    Require(tuple.Items[0].Equals(tuple.Items[1]) && tuple.Items[1].Equals(tuple.Items[2]));
                    var old = (BitString)tuple.Items[3];
                    var next = (BitString)tuple.Items[4];
                    byte[] expectedOld = new byte[257];
                    expectedOld[0] = 7;
                    byte[] expectedNext = [.. expectedOld, 9];
                    Require(old.BitLength == expectedOld.Length * 8 && next.BitLength == expectedNext.Length * 8
                        && old.ToArray().SequenceEqual(expectedOld) && next.ToArray().SequenceEqual(expectedNext));
                    var before = observations[1];
                    Require(before.Owners.Count == 1 && before.Writable.Count == 1 && before.Regular.Count == 0);
                    var owner = before.Owners.Single();
                    Require(owner.Flags == 4 && owner.References == 1);
                    var writer = before.Headers.Single(header => (header.Base & 3) == 3);
                    foreach (int phase in new[] { 2, 3 })
                    {
                        var after = observations[phase];
                        var current = after.Owners.Single();
                        Require(after.Headers.Count == before.Headers.Count && after.Writable.Count == 1 && after.Regular.Count == 0
                            && ReferenceEquals(current.Owner, owner.Owner) && current.BinRef == owner.BinRef
                            && current.Flags == owner.Flags && current.Capacity == owner.Capacity && current.References == 1
                            && after.Headers.Any(header => header == writer));
                    }
                }
                finally
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(SameHeapMailboxCaseNames.Timeout);
                }
                foreach (var owner in observations.Values.SelectMany(value => value.Owners).Select(value => value.Owner).Distinct())
                    await owner.Retired.WaitAsync(SameHeapMailboxCaseNames.Timeout);
            }
    }

    public static async Task ExactCanonicalCapability()
    {
        await using var runtime = NativeOwned516Profile.Create(false);
        ModuleRegistry registry = runtime.Modules;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(ModuleRegistry).GetField(SameHeapMailboxCaseNames.Gate, members)!.GetValue(registry)!;
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(SameHeapMailboxCaseNames.Functions, members)!.GetValue(registry)!;
        MethodInfo selected = typeof(ModuleRegistry).GetMethod(SameHeapMailboxCaseNames.Selection, members)!;
        var originals = new List<(string Name, ErlangFunction Body)>();
        foreach (string name in new[] { ErlangBasicBifNames.Send, ErlangOperators.Send })
        {
            var key = (ErlangModuleNames.Module, name, 2);
            lock (gate)
            {
                ErlangFunction body = functions[key];
                Require(selected.Invoke(registry, [key, body])!.ToString() == SameHeapMailboxCaseNames.Admitted);
                ErlangFunction forwarded = (context, arguments) => body(context, arguments);
                Require(selected.Invoke(registry, [key, forwarded])!.ToString() == SameHeapMailboxCaseNames.Opaque);
                originals.Add((name, body));
            }
        }
        Require(registry.DeleteModule(ErlangModuleNames.Module).Equals(Term.A(ErlangBooleanAtoms.True)));
        foreach (var original in originals)
        {
            var key = (ErlangModuleNames.Module, original.Name, 2);
            registry.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                original.Body
            );
            lock (gate)
                Require(selected.Invoke(registry, [key, original.Body])!.ToString() == SameHeapMailboxCaseNames.Opaque);
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(SameHeapMailboxCaseNames.Failure);
    }
}
