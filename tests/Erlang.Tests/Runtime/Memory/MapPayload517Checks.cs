using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.MapPayload517Names;

namespace Erlang.Tests;

// Unexecuted finite managed-storage controls. They neither manufacture an OTP
// reference nor claim that private slab cells are complete public ERTS words.
internal static class MapPayload517Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task FlatOccurrenceCustody()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, HeapProperty);
            using var roots = (IDisposable)Call(heap, Enter)!;
            int input = (int)Call(roots, Reserve)!;
            Call(
                roots,
                Set,
                input,
                Term.Tuple(Term.A(Payload), Term.I(1))
            );
            Term boxed = (Term)Call(roots, Read, input)!;
            Atom key = Term.A(RepeatedKey);
            var physical = new[]
            {
                new KeyValuePair<Term, Term>(key, boxed),
                new KeyValuePair<Term, Term>(key, Term.I(2))
            };
            var source = (MapTerm)typeof(MapTerm).GetMethod(NativeFlatFactory, Members)!.Invoke(null, [physical, null, null])!;
            using var prepared = (IDisposable)Call(heap, Prepare, source)!;
            var children = ((IReadOnlyList<Term>)Get(prepared, Children)).ToArray();
            MapTerm map;
            int output;
            using (var construction = (IDisposable)Call(heap, Enter)!)
            {
                int before = (int)Get(Get(heap, Snapshot), Blocks);
                bool refused = false;
                lock (Get(heap, HeapGateProperty))
                {
                    try
                    {
                        Call(
                            heap,
                            Create,
                            prepared,
                            construction,
                            children
                        );
                    }
                    catch (InvalidOperationException)
                    {
                        refused = true;
                    }
                }
                Check(refused && (int)Get(Get(heap, Snapshot), Blocks) == before);
                map = (MapTerm)Call(
                    heap,
                    Create,
                    prepared,
                    construction,
                    children
                )!;
                output = (int)Call(roots, Reserve)!;
                Call(
                    roots,
                    SetLocal,
                    output,
                    map
                );
            }
            Check(map.Entries.Count == 2 && map.Get(key).Equals(boxed));
            Check(map.Entries.Select(pair => pair.Value).SequenceEqual(physical.Select(pair => pair.Value)));
            Call(roots, Clear, input);
            Call(heap, Collect);
            Check((long)Get(Get(heap, Snapshot), LivePayloadWords) == 7);
            Check(map.Get(key).Equals(boxed));
            Call(roots, Clear, output);
            Call(heap, Collect);
            Check((int)Get(Get(heap, Snapshot), Blocks) == 0);
            ExpectDisposed(() => map.Get(key));

            return true;
        });
    }

    internal static async Task HamtNodeCustody()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, HeapProperty);
            using var roots = (IDisposable)Call(heap, Enter)!;
            var source = new MapTerm(Enumerable.Range(0, 65).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index + 1))));
            using var prepared = (IDisposable)Call(heap, Prepare, source)!;
            var children = ((IReadOnlyList<Term>)Get(prepared, Children)).ToArray();
            foreach (bool reverse in new[] { false, true })
            {
                int aliasSlot = (int)Call(roots, Reserve)!;
                Term rawSource = ((IReadOnlyList<Term>)Get(prepared, SourceStoredNodes)).First();
                Term sourceGraph = reverse ? Term.Tuple(rawSource, source, source) : Term.Tuple(source, source, rawSource);
                var copied = (TupleTerm)Call(
                    roots,
                    AdoptGuest,
                    aliasSlot,
                    sourceGraph
                )!;
                var aliasMap = (MapTerm)copied.Items[reverse ? 1 : 0];
                Term rawCopy = copied.Items[reverse ? 0 : 2];
                Check(ReferenceEquals(aliasMap, copied.Items[reverse ? 2 : 1]));
                object?[] aliasArguments = [aliasMap, runtime, null];
                Type aliasMaterialization = typeof(ProcessRuntime).Assembly.GetType(MaterializationType)!;
                Check((bool)aliasMaterialization.GetMethod(TryRead, Members)!.Invoke(null, aliasArguments)!);
                object aliasHead = Get(aliasArguments[2]!, Root);
                object first = ((System.Collections.IEnumerable)Get(aliasHead, Children)).Cast<object>().First();
                Check(ReferenceEquals(Get(first, StoredTerm), rawCopy));
                Call(heap, Collect);
                Check(aliasMap.Get(Term.I(0)).Equals(Term.I(1)));
                Call(roots, Clear, aliasSlot);
                Call(heap, Collect);
                Check((int)Get(Get(heap, Snapshot), Blocks) == 0);
            }
            MapTerm map;
            int output;
            using (var construction = (IDisposable)Call(heap, Enter)!)
            {
                map = (MapTerm)Call(
                    heap,
                    Create,
                    prepared,
                    construction,
                    children
                )!;
                output = (int)Call(roots, Reserve)!;
                Call(
                    roots,
                    SetLocal,
                    output,
                    map
                );
            }
            Check(map.Equals(source));
            Check(Enumerable.Range(0, 65).All(index => map.Get(Term.I(index)).Equals(Term.I(index + 1))));
            Check(MapIteration.Entries(map, runtime).Select(pair => pair.Key).SequenceEqual(MapIteration.Entries(source, runtime).Select(pair => pair.Key)));
            Call(heap, Collect);
            Check((int)Get(Get(heap, Snapshot), Blocks) > 65);
            Check(map.Equals(source));
            prepared.Dispose();
            using (var rejected = (IDisposable)Call(heap, Enter)!)
                ExpectDisposed(() => Call(
                    heap,
                    Create,
                    prepared,
                    rejected,
                    children
                ));
            Call(roots, Clear, output);
            Call(heap, Collect);
            Check((int)Get(Get(heap, Snapshot), Blocks) == 0);
            ExpectDisposed(() => map.Get(Term.I(0)));

            return true;
        });
    }

    internal static async Task RawInteriorRetainsOnlySubtree()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, HeapProperty);
            using var roots = (IDisposable)Call(heap, Enter)!;
            var source = new MapTerm(Enumerable.Range(0, 65).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index + 1))));
            using var prepared = (IDisposable)Call(heap, Prepare, source)!;
            MapTerm map;
            int output;
            using (var construction = (IDisposable)Call(heap, Enter)!)
            {
                map = (MapTerm)Call(
                    heap,
                    Create,
                    prepared,
                    construction,
                    ((IReadOnlyList<Term>)Get(prepared, Children)).ToArray()
                )!;
                output = (int)Call(roots, Reserve)!;
                Call(
                    roots,
                    SetLocal,
                    output,
                    map
                );
            }
            var arguments = new object?[] { map, null, null };
            Type materialization = typeof(ProcessRuntime).Assembly.GetType(MaterializationType)!;
            Check((bool)materialization.GetMethod(TryRead, Members)!.Invoke(null, arguments)!);
            object root = Get(arguments[2]!, Root);
            var nodes = ((System.Collections.IEnumerable)Get(root, Children)).Cast<object>();
            Term raw = nodes.Select(node => Get(node, StoredTerm)).OfType<Term>().First(term => term.GetType().Name == BitmapTypeName);
            var rawChildren = (IReadOnlyList<Term>)Get(raw, Children);
            int rawSlot = (int)Call(roots, Reserve)!;
            Call(
                roots,
                SetLocal,
                rawSlot,
                raw
            );
            int all = (int)Get(Get(heap, Snapshot), Blocks);
            Call(roots, Clear, output);
            Call(heap, Collect);
            int retained = (int)Get(Get(heap, Snapshot), Blocks);
            Check(retained > 0 && retained < all);
            _ = rawChildren[0];
            ExpectDisposed(() => map.Get(Term.I(0)));
            Call(roots, Clear, rawSlot);
            Call(heap, Collect);
            Check((int)Get(Get(heap, Snapshot), Blocks) == 0);
            ExpectDisposed(() =>
 {
     _ = rawChildren[0];
 });

            return true;
        });
    }

    internal static async Task PostAllocationHeaderFaultRollsBack()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, HeapProperty);
            using var roots = (IDisposable)Call(heap, Enter)!;
            int input = (int)Call(roots, Reserve)!;
            Call(
                roots,
                Set,
                input,
                Term.Tuple(Term.A(Payload), Term.I(1))
            );
            var source = new MapTerm(Enumerable.Range(0, 65).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index + 1))));
            using var prepared = (IDisposable)Call(heap, Prepare, source)!;
            int before = (int)Get(Get(heap, Snapshot), Blocks);
            var headers = (System.Collections.IDictionary)heap.GetType().GetField(HeadersField, Members)!.GetValue(heap)!;
            long future = checked((long)heap.GetType().GetField(IdentityField, Members)!.GetValue(heap)! + 3);
            Type headerType = typeof(ProcessRuntime).Assembly.GetType(HeaderType)!;
            // Test-only allocator fault: the third real node allocation hits a
            // duplicate header registration. No expected OTP data is invented.
            headers.Add(future, Activator.CreateInstance(headerType));
            using (var construction = (IDisposable)Call(heap, Enter)!)
            {
                try
                {
                    Call(
                        heap,
                        Create,
                        prepared,
                        construction,
                        ((IReadOnlyList<Term>)Get(prepared, Children)).ToArray()
                    );
                    throw new InvalidOperationException(CheckFailed);
                }
                catch (ArgumentException)
                {
                }
            }
            Call(heap, Collect);
            Check((int)Get(Get(heap, Snapshot), Blocks) == before && headers.Count == 0);
            Check(((System.Collections.IDictionary)heap.GetType().GetField(AllocationCandidatesField, Members)!.GetValue(heap)!).Count == 0);
            Check(((TupleTerm)Call(roots, Read, input)!).Items[0].Equals(Term.A(Payload)));

            return true;
        });
    }

    internal static async Task FlatHamtBoundaryRoutes()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, HeapProperty);
            using var roots = (IDisposable)Call(heap, Enter)!;
            int input = (int)Call(roots, Reserve)!;
            Call(
                roots,
                Set,
                input,
                Term.Tuple(Term.A(Payload), Term.I(1))
            );
            Term shared = (Term)Call(roots, Read, input)!;
            int baseline = (int)Get(Get(heap, Snapshot), Blocks);
            foreach (int size in new[] { 0, 1, 2, 31, 32, 33, 65 })
            {
                var source = new MapTerm(Enumerable.Range(0, size).Select(index =>
                    new KeyValuePair<Term, Term>(Term.I(index), shared)));
                int slot = (int)Call(roots, Reserve)!;
                Term keys = (Term?)source.GetType().GetProperty(FlatKeysProperty, Members)!.GetValue(source) ?? Term.Tuple();
                var adopted = (TupleTerm)Call(
                    roots,
                    AdoptGuest,
                    slot,
                    Term.Tuple(
                        source,
                        source,
                        keys,
                        Term.Tuple()
                    )
                )!;
                var map = (MapTerm)adopted.Items[0];
                Check(ReferenceEquals(map, adopted.Items[1]));
                if (size <= 32)
                    Check(ReferenceEquals(map.GetType().GetProperty(FlatKeysProperty, Members)!.GetValue(map), adopted.Items[2]));
                Check(map.Entries.Count == size);
                object? storage = map.GetType().GetProperty(OwnedMapStorage, Members)!.GetValue(map);
                Check((storage is null) == (size == 0));
                if (size != 0)
                    Check(((System.Collections.IDictionary)typeof(MapTerm).GetField(LookupField, Members)!.GetValue(map)!).Count == 0);
                if (size is > 0 and <= 32)
                {
                    int reverseSlot = (int)Call(roots, Reserve)!;
                    var reverse = (TupleTerm)Call(
                        roots,
                        AdoptGuest,
                        reverseSlot,
                        Term.Tuple(keys, source)
                    )!;
                    Check(ReferenceEquals(
                        ((MapTerm)reverse.Items[1]).GetType().GetProperty(FlatKeysProperty, Members)!.GetValue(reverse.Items[1]),
                        reverse.Items[0]
                    ));
                    Call(roots, Clear, reverseSlot);
                }
                Check(MapIteration.Entries(map, runtime).Select(pair => pair.Key)
                    .SequenceEqual(MapIteration.Entries(source, runtime).Select(pair => pair.Key)));
                Call(heap, Collect);
                Check(Enumerable.Range(0, size).All(index => ReferenceEquals(map.Get(Term.I(index)), shared)));
                var portable = (MapTerm)Call(
                    heap,
                    Export,
                    roots,
                    map
                )!;
                Check(portable.GetType().GetProperty(OwnedMapStorage, Members)!.GetValue(portable) is null);
                int imported = (int)Call(roots, Reserve)!;
                Call(
                    roots,
                    Set,
                    imported,
                    portable
                );
                var importedMap = (MapTerm)Call(roots, Read, imported)!;
                Check(importedMap.Equals(map));
                Check((importedMap.GetType().GetProperty(OwnedMapStorage, Members)!.GetValue(importedMap) is null) == (size == 0));
                Call(roots, Clear, imported);
                Call(roots, Clear, slot);
                Call(heap, Collect);
                Check((int)Get(Get(heap, Snapshot), Blocks) == baseline);
                Check(portable.Entries.Count == size && Enumerable.Range(0, size)
                    .All(index => portable.Get(Term.I(index)).Equals(Term.Tuple(Term.A(Payload), Term.I(1)))));
                if (size != 0)
                    ExpectDisposed(() => map.Get(Term.I(0)));
            }

            return true;
        });
    }

    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void ExpectDisposed(Action action)
    {
        try
        {
            action();
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(CheckFailed);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CheckFailed);
    }
}
