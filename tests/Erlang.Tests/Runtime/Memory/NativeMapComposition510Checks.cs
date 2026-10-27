using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeMapOwned510Names;

namespace Erlang.Tests;

internal static class NativeMapComposition510Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task HiddenMajorBinaryAndClose()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term? key = null;
        MapTerm copy = await actor.Invoke(context =>
        {
            object heap = Get(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int tupleSlot = (int)Call(frame, Reserve)!;
            int binarySlot = (int)Call(frame, Reserve)!;
            int hiddenSlot = (int)Call(frame, Reserve)!;
            int mapSlot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                tupleSlot,
                Term.Tuple(Term.A(Hidden), Term.I(510))
            );
            Term boxed = (Term)Call(frame, Read, tupleSlot)!;
            BitString binary = (BitString)Call(
                frame,
                NativeMapComposition510Names.AllocateBinary,
                binarySlot,
                NativeMapComposition510Names.BinaryBytes
            )!;
            Term hidden = (Term)Call(
                frame,
                NativeMapComposition510Names.CreateTuple,
                hiddenSlot,
                new Term[] { boxed, binary }
            )!;
            var seed = new MapTerm(Enumerable.Range(0, 33).Select(index =>
                new KeyValuePair<Term, Term>(Term.I(index), Term.I(0))));
            using var read = (IDisposable)Call(runtime, Acquire, seed)!;
            object index = Get(read, IndexProperty);
            object retention = New(RetentionType, Get(index, Ownership), null);
            object domain = Call(runtime, NativeDomain)!;
            object?[] cloneArguments = [Get(index, Root), domain, retention, hidden, false, null];
            object clonedRoot = typeof(NativeMapOwned510Checks).GetMethod(
                NativeMapComposition510Names.CloneTree, Members)!.Invoke(null, cloneArguments)!;
            Check((bool)cloneArguments[4]!);
            key = (Term)cloneArguments[5]!;
            // The real tree contains the only owned tuple/binary occurrence;
            // canonical and physical metadata deliberately contain no payload.
            KeyValuePair<Term, Term>[] projection = seed.Entries.ToArray();
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
            Call(frame, Clear, tupleSlot);
            Call(frame, Clear, binarySlot);
            Call(frame, Clear, hiddenSlot);
            Call(
                heap,
                NativeMapComposition510Names.Generation,
                true,
                ushort.MaxValue
            );
            Check((long)Get(Get(heap, Snapshot), Words) == 4);
            Check((long)Get(Get(heap, NativeMapComposition510Names.BinarySnapshot),
                NativeMapComposition510Names.ReferenceBytes) == NativeMapComposition510Names.BinaryBytes);
            Check(map.TryGet(key, out Term? current) && current!.Equals(hidden));
            var detached = (MapTerm)Call(
                heap,
                Export,
                frame,
                map
            )!;
            Call(frame, Clear, mapSlot);
            Call(
                heap,
                NativeMapComposition510Names.Generation,
                true,
                ushort.MaxValue
            );
            Check((long)Get(Get(heap, Snapshot), Words) == 0);
            Check((long)Get(Get(heap, NativeMapComposition510Names.BinarySnapshot),
                NativeMapComposition510Names.ReferenceBytes) == 0);

            return detached;
        });
        // Process retirement must not invalidate the copied same-VM hash token
        // or portable leaf, while its logical VM remains open.
        await actor.DisposeAsync();
        Check(copy.TryGet(key!, out Term? surviving));
        var pair = (TupleTerm)surviving!;
        Check(((TupleTerm)pair.Items[0]).Items[0].Equals(Term.A(Hidden)));
        Check(((BitString)pair.Items[1]).ToArray().Length == NativeMapComposition510Names.BinaryBytes);
    }

    private static Type Core(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object Get(object value, string property) => value.GetType().GetProperty(property, Members)!.GetValue(value)!;

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

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NativeMapComposition510Names.Failure);
    }
}
