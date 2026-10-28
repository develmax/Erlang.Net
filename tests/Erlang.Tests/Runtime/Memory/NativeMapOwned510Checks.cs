using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeMapOwned510Names;

namespace Erlang.Tests;

// Managed physical-layout controls, not new original-OTP observations. Parent
// must rerun the hash-bound native505 original/default ETF packet separately.
internal static class NativeMapOwned510Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task FlatDuplicateOccurrences()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int source = (int)Call(frame, Reserve)!;
            int mapRoot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                source,
                Term.Tuple(Term.A(Payload), Term.List(Term.I(1), Term.I(2)))
            );
            Term boxed = (Term)Call(frame, Read, source)!;
            var entries = new[]
            {
                new KeyValuePair<Term,Term>(new BitString([1]),boxed),
                new KeyValuePair<Term,Term>(new BitString([1]),boxed),
                new KeyValuePair<Term,Term>(new BitString([1]),boxed)
            };
            MapTerm map = MaterializeFlat(runtime, entries);
            Check(map.Entries.Count == 3);
            Call(
                frame,
                SetLocal,
                mapRoot,
                map
            );
            Call(frame, Clear, source);
            Call(heap, Collect);
            Check(Live(heap) > 0);
            var copy = (MapTerm)Call(
                heap,
                Export,
                frame,
                map
            )!;
            var physical = PhysicalRows(copy);
            Check(copy.Entries.Count == 3 && physical.Count == 3);
            Check(!ReferenceEquals(physical[0].Value, physical[1].Value)
                && !ReferenceEquals(physical[1].Value, physical[2].Value));
            Check(physical.All(pair => pair.Value.Equals(boxed)));
            RequirePortable(copy);
            Call(frame, Clear, mapRoot);
            Call(heap, Collect);
            Check(Live(heap) == 0);
            Check(physical.All(pair => ((TupleTerm)pair.Value).Items[0].Equals(Term.A(Payload))));

            return true;
        });
    }

    internal static async Task HiddenHamtRootsAndTopology()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Get(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int visibleSlot = (int)Call(frame, Reserve)!;
            int hiddenSlot = (int)Call(frame, Reserve)!;
            int mapSlot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                visibleSlot,
                Term.Tuple(Term.A(Payload), Term.I(1))
            );
            Call(
                frame,
                Set,
                hiddenSlot,
                Term.Tuple(Term.A(Hidden), Term.I(2), Term.I(3))
            );
            Term visible = (Term)Call(frame, Read, visibleSlot)!;
            Term hidden = (Term)Call(frame, Read, hiddenSlot)!;
            var seed = new MapTerm(Enumerable.Range(0, 33).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(0))));
            using var read = (IDisposable)Call(runtime, Acquire, seed)!;
            object index = Get(read, IndexProperty);
            object originalRoot = Get(index, Root);
            object retention = New(RetentionType, Get(index, Ownership), null);
            object domain = Call(runtime, NativeDomain)!;
            bool replaced = false;
            Term? hiddenKey = null;
            object clonedRoot = CloneTree(
                originalRoot,
                domain,
                retention,
                hidden,
                ref replaced,
                ref hiddenKey
            );
            var projection = seed.Entries.Select(pair => new KeyValuePair<Term, Term>(pair.Key, visible)).ToArray();
            // Intentional modeled mismatch: a physical HAMT leaf payload is
            // absent from canonical/physical-entry metadata. All views matter.
            object layout = New(
                LayoutType,
                runtime,
                domain,
                projection,
                clonedRoot,
                retention
            );
            var map = (MapTerm)Call(
                Core(MaterializationType),
                Publish,
                runtime,
                layout,
                projection,
                clonedRoot,
                projection,
                projection,
                null
            )!;
            Call(
                frame,
                SetLocal,
                mapSlot,
                map
            );
            Call(frame, Clear, visibleSlot);
            Call(frame, Clear, hiddenSlot);
            Call(heap, Collect);
            Check(Live(heap) == 5);
            Check(map.TryGet(hiddenKey!, out Term? actual) && ReferenceEquals(actual, hidden));
            var copied = (MapTerm)Call(
                heap,
                Export,
                frame,
                map
            )!;
            object copiedLayout = Layout(copied);
            Check(ReferenceEquals(Get(copiedLayout, Domain), domain));
            Check(ReferenceEquals(Get(copiedLayout, HashRetention), retention));
            ShapeEqual(clonedRoot, Get(copiedLayout, Root));
            Check(copied.TryGet(hiddenKey!, out Term? copiedHidden) && copiedHidden!.Equals(hidden));
            Check(!ReferenceEquals(copiedHidden, hidden));
            RequirePortable(copied);
            Call(frame, Clear, mapSlot);
            Call(heap, Collect);
            Check(Live(heap) == 0);
            Check(((TupleTerm)copiedHidden!).Items[0].Equals(Term.A(Hidden)));

            return true;
        });
    }

    internal static async Task ForeignDomainFailsClosed()
    {
        await using var source = OwnedGuest506RuntimeFactory.Create();
        await using var target = OwnedGuest506RuntimeFactory.Create();
        MapTerm map = MaterializeFlat(source, [new(Term.I(1), Term.Tuple(Term.I(2)))]);
        try
        {
            _ = Call(
                Core(CopyType),
                Copy,
                target,
                map
            );
            throw new InvalidOperationException(Failure);
        }
        catch (NotSupportedException)
        {
        }
    }

    private static MapTerm MaterializeFlat(ProcessRuntime runtime, KeyValuePair<Term, Term>[] entries)
    {
        object resolved = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(Core(NodeType), typeof(Term)))!;

        return (MapTerm)Call(
            Core(MaterializationType),
            Build,
            runtime,
            entries,
            null,
            null,
            resolved
        )!;
    }

    private static object Layout(MapTerm map)
    {
        object?[] arguments = [map, null, null];
        Check((bool)Core(MaterializationType).GetMethod(TryRead, Members)!.Invoke(null, arguments)!);

        return arguments[2]!;
    }

    private static IReadOnlyList<KeyValuePair<Term, Term>> PhysicalRows(MapTerm map)
        => (IReadOnlyList<KeyValuePair<Term, Term>>)Get(Layout(map), Physical);

    private static object CloneTree(
        object source,
        object domain,
        object retention,
        Term hidden,
        ref bool replaced,
        ref Term? hiddenKey
    )
    {
        object? leaf = source.GetType().GetProperty(Leaf, Members)!.GetValue(source);
        if (leaf is KeyValuePair<Term, Term> pair && !replaced)
        {
            leaf = new KeyValuePair<Term, Term>(pair.Key, hidden);
            hiddenKey = pair.Key;
            replaced = true;
        }
        object[] original = ((IEnumerable)Get(source, Children)).Cast<object>().ToArray();
        Array children = Array.CreateInstance(Core(TreeType), original.Length);
        for (int index = 0; index < original.Length; index++)
            children.SetValue(CloneTree(
                original[index],
                domain,
                retention,
                hidden,
                ref replaced,
                ref hiddenKey
            ), index);

        return New(
            TreeType,
            leaf,
            children,
            Get(source, Bitmap),
            Get(source, Collision),
            domain,
            retention,
            Get(source, Head)
        );
    }

    private static void ShapeEqual(object left, object right)
    {
        Check(Get(left, Bitmap).Equals(Get(right, Bitmap)) && Get(left, Collision).Equals(Get(right, Collision))
            && Get(left, Head).Equals(Get(right, Head)));
        object[] first = ((IEnumerable)Get(left, Children)).Cast<object>().ToArray();
        object[] second = ((IEnumerable)Get(right, Children)).Cast<object>().ToArray();
        Check(first.Length == second.Length);
        Check((left.GetType().GetProperty(Leaf, Members)!.GetValue(left) is null)
            == (right.GetType().GetProperty(Leaf, Members)!.GetValue(right) is null));
        for (int index = 0; index < first.Length; index++)
            ShapeEqual(first[index], second[index]);
    }

    private static void RequirePortable(Term source)
    {
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(source);
        while (pending.TryPop(out var term))
        {
            if (!seen.Add(term)) continue;
            PropertyInfo? storage = term.GetType().GetProperty(Storage, Members);
            Check(storage?.GetValue(term) is null);
            foreach (Term child in (Term[])Call(Core(RootsType), Children, term)!)
                pending.Push(child);
        }
    }

    private static long Live(object heap) => (long)Get(Get(heap, Snapshot), Words);

    private static Type Core(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object Get(object target, string property) => target.GetType().GetProperty(property, Members)!.GetValue(target)!;

    private static object New(string name, params object?[] arguments) => Activator.CreateInstance(
        Core(name),
        Members,
        null,
        arguments,
        null
    )!;

    private static object? Call(object target, string method, params object?[] arguments)
    {
        Type type = target is Type selected ? selected : target.GetType();
        MethodInfo member = type.GetMethods(Members).Single(candidate => candidate.Name == method
            && candidate.GetParameters().Length == arguments.Length
            && candidate.GetParameters().Select((parameter, index) => arguments[index] is null
                || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(value => value));
        try
        {
            return member.Invoke(target is Type ? null : target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
