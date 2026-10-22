using Erlang;

namespace Erlang.Tests;

internal static class FlatMapEtfHostChecks
{
    internal static async Task OpposingUniqueNativeGolden()
    {
        await using var runtime = new ProcessRuntime();
        byte[] original = FlatMapEtfHostAssets.Read();
        Term imported = FlatMapEtfHostAssets.Import(runtime);
        FlatMapEtfHostWire.Equal(original, ExternalTermFormat.Encode(imported, FlatMapEtfHostWire.Options(runtime)));
        MapTerm map = FlatMapEtfHostAssets.Pair(runtime);
        var oldOrder = FlatMapEtfHostAssets.OldOrder(map);
        var first = (FunctionTerm)oldOrder[0].Key;
        var second = (FunctionTerm)oldOrder[1].Key;
        FlatMapEtfHostWire.Require(first.LocalIdentity!.Unique.ToArray().All(value => value == byte.MaxValue)
            && second.LocalIdentity!.Unique.ToArray().All(value => value == byte.MinValue));
        byte[] expected = FlatMapEtfHostWire.Map(runtime, oldOrder);
        foreach (var rows in new[] { oldOrder, oldOrder.Reverse().ToArray() })
            FlatMapEtfHostWire.Equal(expected, ExternalTermFormat.Encode(new MapTerm(rows), FlatMapEtfHostWire.Options(runtime)));
    }

    internal static async Task ExactNumericBindings()
    {
        await using var runtime = new ProcessRuntime();
        var rows = FlatMapEtfHostAssets.OldOrder(FlatMapEtfHostAssets.Pair(runtime)).ToList();
        rows.Add(new(Term.I(1), Term.A(FlatMapEtfHostNames.Marker)));
        rows.Add(new(new FloatTerm(1.0), Term.I(2)));
        var map = new MapTerm(rows);
        FlatMapEtfHostWire.Require(map.Entries.Count == 4
            && map.Get(Term.I(1)).Equals(Term.A(FlatMapEtfHostNames.Marker))
            && map.Get(new FloatTerm(1.0)).Equals(Term.I(2)));
        FlatMapEtfHostWire.Bindings(runtime, map);
        var expected = rows.OrderBy(row => row.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, true))).ToArray();
        FlatMapEtfHostWire.Equal(FlatMapEtfHostWire.Map(runtime, expected), ExternalTermFormat.Encode(map, FlatMapEtfHostWire.Options(runtime)));
    }

    internal static async Task EqualOldUniqueDistinctCode()
    {
        await using var runtime = new ProcessRuntime();
        FunctionTerm[] originals = FlatMapEtfHostAssets.OldOrder(FlatMapEtfHostAssets.Pair(runtime))
            .Select(pair => (FunctionTerm)pair.Key).ToArray();

        FunctionTerm Copy(FunctionTerm original) => new(
            original.LocalIdentity! with { OldUnique = FlatMapEtfHostNames.LegacyTie },
            original.Arity,
            original.Environment,
            (_, _) => throw new InvalidOperationException(FlatMapEtfHostNames.UnexpectedInvocation)
        );
        FunctionTerm first = Copy(originals[0]), second = Copy(originals[1]);
        var map = new MapTerm([new(first, Term.I(1)), new(second, Term.I(2))]);
        FlatMapEtfHostWire.Require(!first.Equals(second) && map.Entries.Count == 2
            && map.Get(first).Equals(Term.I(1)) && map.Get(second).Equals(Term.I(2)));
        FlatMapEtfHostWire.Bindings(runtime, map);
    }

    internal static async Task DeterministicBaseline()
    {
        await using var runtime = new ProcessRuntime();
        MapTerm map = FlatMapEtfHostAssets.Pair(runtime);
        var oldOrder = FlatMapEtfHostAssets.OldOrder(map);
        var canonicalCodeOrder = oldOrder.Reverse().ToArray();
        byte[] expectedBaseline = FlatMapEtfHostWire.Map(runtime, canonicalCodeOrder, true);
        FlatMapEtfHostWire.Equal(expectedBaseline, ExternalTermFormat.Encode(map, FlatMapEtfHostWire.Options(runtime, true)));
        FlatMapEtfHostWire.Require(!expectedBaseline.AsSpan().SequenceEqual(ExternalTermFormat.Encode(map, FlatMapEtfHostWire.Options(runtime))));
    }

    internal static async Task CardinalityBoundaries()
    {
        await using var runtime = new ProcessRuntime();
        var pair = FlatMapEtfHostAssets.OldOrder(FlatMapEtfHostAssets.Pair(runtime));
        foreach (int count in new[] { 1, 2, FlatMapEtfHostNames.FlatMaximum, FlatMapEtfHostNames.FlatMaximum + 1 })
        {
            var rows = pair.Take(Math.Min(count, 2)).ToList();
            for (int index = rows.Count; index < count; index++) rows.Add(new(Term.I(index), Term.I(-index)));
            var map = new MapTerm(rows);
            var expected = rows.Where(row => row.Key is Integer).OrderBy(row => ((Integer)row.Key).Value).ToList();
            expected.AddRange(count > FlatMapEtfHostNames.FlatMaximum ? pair.Reverse() : pair.Take(Math.Min(count, 2)));
            FlatMapEtfHostWire.Equal(FlatMapEtfHostWire.Map(runtime, expected), ExternalTermFormat.Encode(map, FlatMapEtfHostWire.Options(runtime)));
            FlatMapEtfHostWire.Bindings(runtime, map);
        }
    }

    internal static async Task NestedMapValue()
    {
        await using var runtime = new ProcessRuntime();
        MapTerm child = FlatMapEtfHostAssets.Pair(runtime);
        Atom key = Term.A(FlatMapEtfHostNames.Marker);
        var outer = new MapTerm([new(key, child)]);
        byte[] expected = FlatMapEtfHostWire.RawMap([(ExternalTermFormat.Encode(key),
            FlatMapEtfHostWire.Map(runtime, FlatMapEtfHostAssets.OldOrder(child)))]);
        FlatMapEtfHostWire.Equal(expected, ExternalTermFormat.Encode(outer, FlatMapEtfHostWire.Options(runtime)));
        FlatMapEtfHostWire.Bindings(runtime, outer);
    }

    internal static async Task ScalarEncodingUnchanged()
    {
        await using var runtime = new ProcessRuntime();
        var pair = FlatMapEtfHostAssets.OldOrder(FlatMapEtfHostAssets.Pair(runtime));
        Term value = Term.Tuple(pair[0].Key, pair[1].Key, Term.List(Term.I(1), Term.A(FlatMapEtfHostNames.Marker)));
        byte[] normal = ExternalTermFormat.Encode(value, FlatMapEtfHostWire.Options(runtime));
        byte[] deterministic = ExternalTermFormat.Encode(value, FlatMapEtfHostWire.Options(runtime, true));
        FlatMapEtfHostWire.Equal(normal, deterministic);
        FlatMapEtfHostWire.Require(runtime.ImportExternalTerm(normal).Term.Equals(value));
    }

    internal static async Task ConstructionNodeSnapshot()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            Pid local = context.Self;
            var foreign = new Pid(FlatMapEtfHostNames.ForeignNode, local.Id);
            var rows = new[] { new KeyValuePair<Term, Term>(local, Term.I(1)), new(foreign, Term.I(2)) };
            var savedOrder = rows.OrderBy(row => row.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, true))).ToArray();
            int before = Math.Sign(TermOrder.Compare(local, foreign, true));
            var map = new MapTerm(rows);
            RuntimeNodeAdmissionHostSetup.Attach(context, FlatMapEtfHostNames.NewNode);
            FlatMapEtfHostWire.Require(before != 0 && Math.Sign(TermOrder.Compare(local, foreign, true)) == -before);
            FlatMapEtfHostWire.Equal(FlatMapEtfHostWire.Map(runtime, savedOrder), ExternalTermFormat.Encode(map, FlatMapEtfHostWire.Options(runtime)));
            FlatMapEtfHostWire.Require(map.Get(local).Equals(Term.I(1)) && map.Get(foreign).Equals(Term.I(2)));

            return true;
        });
    }
}
