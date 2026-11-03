using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.MapPayload517Names;

namespace Erlang.Tests;

internal static class OwnedWords526Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task PhysicalHeadersAndRetirement()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object? nativeCells = null;
        await actor.Invoke(context =>
        {
            object heap = Get(context, HeapProperty);
            using var roots = (IDisposable)Call(heap, Enter)!;
            Term shared = Term.Tuple(Term.I(7), Nil.Value);
            var source = new MapTerm([new(Term.I(1), shared), new(Term.I(2), shared)]);
            var sourceKeys = (Term)Get(source, FlatKeysProperty);
            int slot = (int)Call(roots, Reserve)!;
            var graph = (TupleTerm)Call(
                roots,
                AdoptGuest,
                slot,
                Term.Tuple(source, source, sourceKeys)
            )!;
            var map = (MapTerm)graph.Items[0];
            Check(ReferenceEquals(map, graph.Items[1]) && ReferenceEquals(Get(map, FlatKeysProperty), graph.Items[2]));
            object snapshot = Get(heap, OwnedWords526Names.PhysicalSnapshot);
            // Reachable real words: outer tuple4 + map5 + keys tuple3 + value tuple3.
            Check((long)Get(snapshot, OwnedWords526Names.Physical) == 15);
            Check((long)Get(snapshot, OwnedWords526Names.Headers) == 5);
            Check((long)Get(snapshot, OwnedWords526Names.PayloadCells) == 10);
            Check((long)Get(snapshot, OwnedWords526Names.Handles) == 10);
            Check(Size(
                heap,
                context,
                graph,
                false
            ) == 15);
            // flat_size counts repeated incoming edges, including the key tuple.
            Check(Size(
                heap,
                context,
                graph,
                true
            ) == 35);
            object payload = Get(Get(map, OwnedMapStorage), OwnedWords526Names.MapRootPayload);
            object slab = Get(payload, OwnedWords526Names.Slab);
            nativeCells = Get(slab, OwnedWords526Names.Cells);
            int offset = (int)Get(payload, OwnedWords526Names.AllocationOffset);
            nuint first;
            lock (Get(heap, HeapGateProperty))
            {
                first = (nuint)Call(nativeCells, OwnedWords526Names.ReadHeader, offset)!;
                Check(first == 0x12c && (nuint)Call(nativeCells, OwnedWords526Names.ReadHeader, offset + 1)! == 2);
            }
            bool refused = false;
            try
            {
                lock (Get(heap, HeapGateProperty))
                    Call(
                        nativeCells,
                        OwnedWords526Names.WriteHeaders,
                        new nuint[] { 0 },
                        offset
                    );
                try
                {
                    _ = Size(
                        heap,
                        context,
                        graph,
                        true
                    );
                }
                catch (InvalidOperationException)
                {
                    refused = true;
                }
            }
            finally
            {
                lock (Get(heap, HeapGateProperty))
                    Call(
                        nativeCells,
                        OwnedWords526Names.WriteHeaders,
                        new nuint[] { first },
                        offset
                    );
            }
            Check(refused && Size(
                heap,
                context,
                graph,
                false
            ) == 15);
            lock (Get(heap, HeapGateProperty))
            {
                object header = Call(heap, OwnedWords526Names.AllocateHeader, new nuint[] { 0x12c, 2 })!;
                Check((long)Get(Get(heap, OwnedWords526Names.PhysicalSnapshot), OwnedWords526Names.Physical) == 17);
                Call(heap, OwnedWords526Names.ReleaseHeader, header);
            }
            Check((long)Get(Get(heap, OwnedWords526Names.PhysicalSnapshot), OwnedWords526Names.Physical) == 15);
            Call(roots, Clear, slot);
            Call(heap, Collect);
            snapshot = Get(heap, OwnedWords526Names.PhysicalSnapshot);
            Check((long)Get(snapshot, OwnedWords526Names.Physical) == 0
                && (long)Get(snapshot, OwnedWords526Names.Headers) == 0
                && (long)Get(snapshot, OwnedWords526Names.Handles) == 0);

            return true;
        });
        await actor.DisposeAsync();
        await runtime.DisposeAsync();
        Check(nativeCells is not null && (bool)Get(nativeCells, OwnedWords526Names.Disposed));
    }

    private static ulong Size(
        object heap,
        ProcessContext context,
        Term source,
        bool flat
    )
    {
        object?[] arguments = [context, source, flat, null];
        Check((bool)Call(heap, OwnedWords526Names.TrySize, arguments)!);

        return (ulong)arguments[3]!;
    }

    private static object Get(object target, string property) => target.GetType().GetProperty(property, Members)!.GetValue(target)!;

    private static object? Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CheckFailed);
    }
}
