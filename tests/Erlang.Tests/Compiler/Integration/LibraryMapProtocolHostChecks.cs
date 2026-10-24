using System.Security.Cryptography;
using Erlang;
using static Erlang.Tests.LibraryMapProtocolNames;

namespace Erlang.Tests;

internal static class LibraryMapProtocolHostChecks
{
    internal static async Task RunIndependent()
    {
        await using ProcessRuntime runtime = await Start();
        PreparedBeamImage[] images = new[] { Subject, Observer }.Select(module => runtime.Modules.PrepareBeam(
            Read(module + BeamExtension),
            new BeamDependencySelection(Modules.Concat([Internal, ErlangModuleNames.Module, Subject, Observer]))
        )).ToArray();
        Require(runtime.Modules.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term values = await runtime.Modules.Call(context, Observer, Run);
            CheckRows(runtime.ImportExternalTerm(Read(ValuesFile)).Term, values);
            Term[] originalProfiles = Cons.Items(runtime.ImportExternalTerm(Read(StacksFile)).Term).ToArray();
            Term[] profiles = Cons.Items(await runtime.Modules.Call(context, Observer, Stacks)).ToArray();
            Require(profiles.Length == Profiles && originalProfiles.Length == Profiles);
            foreach ((Term original, Term actual) in originalProfiles.Zip(profiles))
            {
                Require(original is TupleTerm { Items.Count: 3 } && actual is TupleTerm { Items.Count: 3 });
                var first = (TupleTerm)original;
                var second = (TupleTerm)actual;
                Require(first.Items[0].Equals(second.Items[0]) && first.Items[1].Equals(second.Items[1]));
                CheckRows(first.Items[2], second.Items[2]);
            }

            return true;
        });
    }

    internal static async Task OwnLayout()
    {
        Atom first = Term.A(FirstAtom);
        Atom second = Term.A(SecondAtom);
        Term[] keys = new[] { first, second }.OrderBy(key => AtomNameRegistry.IndexOf(key.Name)).Cast<Term>().ToArray();
        var map = new MapTerm(new Dictionary<Term, Term> { [first] = Term.I(1), [second] = Term.I(2) });
        Term pairs = Cons.From(keys.Select(key => Term.Tuple(key, map.Get(key))));
        await using ProcessRuntime runtime = await Start();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Require((await runtime.Modules.Call(
                context,
                Maps,
                Keys,
                map
            )).Equals(Cons.From(keys)));
            Require((await runtime.Modules.Call(
                context,
                Maps,
                Values,
                map
            )).Equals(Cons.From(keys.Select(map.Get))));
            Require((await runtime.Modules.Call(
                context,
                Maps,
                MapToList,
                map
            )).Equals(pairs));
            Require((await runtime.Modules.Call(
                context,
                Properties,
                PropertiesFromMap,
                map
            )).Equals(pairs));
            Require((await runtime.Modules.Call(
                context,
                Properties,
                PropertiesKeys,
                Term.List(first, second)
            )).Equals(Cons.From(keys)));
            foreach (int path in new[] { 0, 1, keys.Length })
            {
                Term iterator = await runtime.Modules.Call(
                    context,
                    Internal,
                    Next,
                    Term.I(path),
                    map,
                    Term.A(IteratorMode)
                );
                foreach (Term key in keys)
                {
                    Require(iterator is TupleTerm { Items.Count: 3 });
                    var pair = (TupleTerm)iterator;
                    Require(pair.Items[0].Equals(key) && pair.Items[1].Equals(map.Get(key)));
                    iterator = pair.Items[2];
                }
                Require(iterator.Equals(Term.A(None)));
                Term accumulated = await runtime.Modules.Call(
                    context,
                    Internal,
                    Next,
                    Term.I(path),
                    map,
                    Nil.Value
                );
                Require(accumulated.Equals(pairs));
                foreach (Term seed in new Term[] { Term.List(Term.A(AccumulatorSeed)), new Cons(Term.A(AccumulatorSeed), Term.A(AccumulatorTail)) })
                {
                    Term expected = seed;
                    foreach (Term key in keys.Reverse())
                        expected = new Cons(Term.Tuple(key, map.Get(key)), expected);
                    Term actual = await runtime.Modules.Call(
                        context,
                        Internal,
                        Next,
                        Term.I(path),
                        map,
                        seed
                    );
                    Require(actual.Equals(expected));
                    foreach (Term _ in keys)
                        actual = ((Cons)actual).Tail;
                    Require(ReferenceEquals(actual, seed));
                }
            }
            var large = new MapTerm(Enumerable.Range(1, FlatMapLimit + 1).Select(number => new KeyValuePair<Term, Term>(Term.I(number), Term.I(-number))));
            Term cursor = await runtime.Modules.Call(
                context,
                Internal,
                Next,
                Term.I(0),
                large,
                Term.A(IteratorMode)
            );
            var visited = new HashSet<Term>();
            for (int index = 0; index < FlatMapLimit; index++)
            {
                Require(cursor is TupleTerm { Items.Count: 3 });
                var pair = (TupleTerm)cursor;
                Require(visited.Add(pair.Items[0]) && pair.Items[1].Equals(large.Get(pair.Items[0])));
                cursor = pair.Items[2];
            }
            Require(cursor is Cons { Head: Integer, Tail: MapTerm });
            var continuation = (Cons)cursor;
            Require(ReferenceEquals(continuation.Tail, large));
            Term final = await runtime.Modules.Call(
                context,
                Internal,
                Next,
                continuation.Head,
                continuation.Tail,
                Term.A(IteratorMode)
            );
            Require(final is TupleTerm { Items.Count: 3 } tail && visited.Add(tail.Items[0])
                && tail.Items[1].Equals(large.Get(tail.Items[0])) && tail.Items[2].Equals(Term.A(None))
                && visited.Count == large.Entries.Count);

            return true;
        });
    }

    private static Task<ProcessRuntime> Start()
        => ProcessRuntime.CreateAsync(new RuntimeLibrarySelection(Modules.Select(module => RuntimeModuleSelection.Beam(module,
            LibraryCollectionsHostChecks.Read(module + BeamExtension), LibraryCollectionsNames.Dependencies[module]))));

    private static void CheckRows(Term original, Term actual)
    {
        Term[] first = Cons.Items(original).ToArray();
        Term[] second = Cons.Items(actual).ToArray();
        Require(first.Length == CaseCount && second.Length == CaseCount);
        int compared = 0;
        foreach ((Term expected, Term result) in first.Zip(second))
        {
            Require(expected is TupleTerm { Items.Count: 2 } && result is TupleTerm { Items.Count: 2 });
            var left = (TupleTerm)expected;
            var right = (TupleTerm)result;
            Require(left.Items[0].Equals(right.Items[0]) && left.Items[0] is Atom);
            if (StateDependentCases.Contains(((Atom)left.Items[0]).Name))
                continue;
            Require(left.Equals(right));
            compared++;
        }
        Require(compared == IndependentCount);
    }

    private static byte[] Read(string name)
    {
        using Stream stream = typeof(LibraryMapProtocolHostChecks).Assembly.GetManifestResourceStream(Prefix + name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == Digests[name]);

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
