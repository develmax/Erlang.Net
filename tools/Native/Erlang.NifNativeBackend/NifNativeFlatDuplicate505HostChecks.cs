using static Erlang.NifNativeBackend.NifNativeFlatDuplicate505HostNames;

namespace Erlang.NifNativeBackend;

internal static class NifNativeFlatDuplicate505HostChecks
{
    internal static void MaterializedSameReferenceRows(ProcessContext context)
    {
        Term key = Key();
        var rows = Rows(key);
        var pending = new NifNativeMapNode504(
            rows.Select(pair =>
            new KeyValuePair<NifNativeTermValue, NifNativeTermValue>(new(pair.Key), new(pair.Value))).ToArray(),
            context.Runtime
        );
        var map = (MapTerm)NifNativeTermGraph.Resolve(new(pending));
        VerifyReentry(map, key, context.Runtime);
    }

    internal static void FactoryRowsAndPeerEquality(ProcessContext context)
    {
        Term key = Key();
        var rows = Rows(key);
        // Deliberately no Core CWT entry: this exercises the occurrence-aware
        // fallback, not only the materialization metadata fast path.
        var factoryMethod = typeof(MapTerm).GetMethod(
            FactoryMethod,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        ) ?? throw new InvalidOperationException(Failure);
        var factory = (MapTerm)factoryMethod.Invoke(null, [rows, null, null])!;
        VerifyReentry(factory, key, context.Runtime);
        var pending = new NifNativeMapNode504(
            rows.Select(pair =>
            new KeyValuePair<NifNativeTermValue, NifNativeTermValue>(new(pair.Key), new(pair.Value))).ToArray(),
            context.Runtime
        );
        var materialized = (MapTerm)NifNativeTermGraph.Resolve(new(pending));
        Require(materialized.Equals(factory) && factory.Equals(materialized));
    }

    private static void VerifyReentry(MapTerm map, Term key, ProcessRuntime runtime)
    {
        Require(NifNativeMapGraph504.TryNode(new(map), runtime, out var node));
        var duplicates = node.Entries.Where(pair => ReferenceEquals(pair.Key.StaticTerm, key)).ToArray();
        Require(duplicates.Length == 2
            && NifNativeTermGraph.Resolve(duplicates[0].Value).Equals(Term.I(Replacement))
            && NifNativeTermGraph.Resolve(duplicates[1].Value).Equals(Term.I(Retained)));
        var removed = NifNativeMapGraph504.Change(
            node,
            new(key),
            default,
            NifNativeMapMutation504Kind.Remove,
            runtime,
            out bool found
        );
        Require(found && removed.Entries.Count == 31
            && NifNativeMapGraph504.TryGet(
                removed,
                key,
                runtime,
                out var remaining
            )
            && NifNativeTermGraph.Resolve(remaining.Value).Equals(Term.I(Retained)));
        var updated = NifNativeMapGraph504.Change(
            node,
            new(key),
            new(Term.I(Update)),
            NifNativeMapMutation504Kind.Update,
            runtime,
            out found
        );
        var changed = updated.Entries.Where(pair => ReferenceEquals(pair.Key.StaticTerm, key)).ToArray();
        Require(found && changed.Length == 2
            && NifNativeTermGraph.Resolve(changed[0].Value).Equals(Term.I(Update))
            && NifNativeTermGraph.Resolve(changed[1].Value).Equals(Term.I(Retained)));
    }

    private static KeyValuePair<Term, Term>[] Rows(Term key)
        => new[]
        {
            new KeyValuePair<Term, Term>(key, Term.I(Replacement)),
            new KeyValuePair<Term, Term>(key, Term.I(Retained))
        }.Concat(Enumerable.Range(2, 30).Select(value =>
            new KeyValuePair<Term, Term>(Term.I(value), Term.I(1000 + value)))).ToArray();

    private static FunctionTerm Key() => new(
        new LocalFunctionIdentity(
            Term.A(Module),
            null,
            FunctionIndex,
            OldUnique,
            new BitString(Enumerable.Repeat((byte)17, UniqueBytes).ToArray())
        ),
        0,
        [Term.I(17)],
        static (_, _) => throw new InvalidOperationException(Failure)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
