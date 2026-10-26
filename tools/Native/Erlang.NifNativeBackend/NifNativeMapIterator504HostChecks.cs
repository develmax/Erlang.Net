using System.Runtime.InteropServices;
using static Erlang.NifNativeBackend.NifNativeMapIterator504HostNames;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeMapIterator504HostChecks
{
    internal static void ShapesAndBoundaries(ProcessContext context)
    {
        Require(sizeof(NifNativeMapIterator504) == IteratorWords * sizeof(nuint));
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            failure
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint* storage = stackalloc nuint[GuardedWords];
        foreach (int size in Sizes)
        {
            var map = new MapTerm(Enumerable.Range(1, size).Select(value =>
                new KeyValuePair<Term, Term>(Term.I(value), Term.Tuple(Term.I(value), Term.I(-value)))));
            var expected = MapIteration.Entries(map, context.Runtime);
            nuint mapHandle = owner.Add(map);
            foreach (int direction in new[] { NifNativeMapIterator504Names.First, NifNativeMapIterator504Names.Last })
            {
                Fill(storage);
                var iterator = (NifNativeMapIterator504*)(storage + 1);
                Require(Create(
                    table,
                    0,
                    mapHandle,
                    iterator,
                    direction
                ) == 1);
                Require(iterator->Size == (nuint)size && owner.MapIteratorCount504 == 1);
                bool forward = direction == NifNativeMapIterator504Names.First;
                Require(iterator->Index == (forward ? 1u : (nuint)size));
                var actual = new List<KeyValuePair<Term, Term>>();
                while (ReadPair(
                    table,
                    owner,
                    iterator,
                    out var pair
                ))
                {
                    actual.Add(pair);
                    int moved = Move(
                        table,
                        0,
                        iterator,
                        forward
                    );
                    Require(moved == (actual.Count < size ? 1 : 0));
                }
                Require(actual.SequenceEqual(forward ? expected : expected.Reverse()));
                Require(Predicate(
                    table,
                    0,
                    iterator,
                    TailSlot
                ) == (forward || size == 0 ? 1 : 0));
                Require(Predicate(
                    table,
                    0,
                    iterator,
                    HeadSlot
                ) == (!forward || size == 0 ? 1 : 0));
                nuint key = Unchanged, value = Unchanged;
                Require(Pair(
                    table,
                    0,
                    iterator,
                    &key,
                    &value
                ) == 0 && key == Unchanged && value == Unchanged);
                Require(Pair(
                    table,
                    0,
                    iterator,
                    null,
                    null
                ) == 0);
                Require(Move(
                    table,
                    0,
                    iterator,
                    forward
                ) == 0);
                Require(Move(
                    table,
                    0,
                    iterator,
                    !forward
                ) == (size == 0 ? 0 : 1));
                if (size != 0)
                {
                    Require(ReadPair(
                        table,
                        owner,
                        iterator,
                        out var reentered
                    ));
                    Require(reentered.Equals(forward ? expected[^1] : expected[0]));
                }
                Destroy(table, 0, iterator);
                Require(owner.MapIteratorCount504 == 0);
                Require(storage[0] == Before && storage[GuardedWords - 1] == After);
            }
        }
        Fill(storage);
        foreach (Term nonmap in new Term[] { Nil.Value, Term.I(1), new FloatTerm(1), Term.Tuple(), Term.List(Term.I(1)), new BitString([]) })
        {
            Require(Create(
                table,
                0,
                owner.Add(nonmap),
                (NifNativeMapIterator504*)(storage + 1),
                NifNativeMapIterator504Names.First
            ) == 0);
            Require(new ReadOnlySpan<nuint>(storage + 1, IteratorWords).ToArray().All(value => value == Unchanged));
            Require(Create(
                table,
                0,
                owner.Add(nonmap),
                null,
                NifNativeMapIterator504Names.First
            ) == 0);
        }
        foreach (int invalid in new[] { 0, 3 })
        {
            Require(Create(
                table,
                0,
                owner.Add(new MapTerm([new(Term.I(1), Term.I(2))])),
                (NifNativeMapIterator504*)(storage + 1),
                invalid
            ) == 0);
            Require(new ReadOnlySpan<nuint>(storage + 1, IteratorWords).ToArray().All(value => value == Unchanged));
            Require(owner.MapIteratorCount504 == 0);
        }
        failure.ThrowPending();
    }

    internal static void HintsAndLifetime(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        var image = new ModuleCodeImage(Module);
        using var owner = new NifNativeCallEnvironment(context.Runtime, image);
        using var unrelated = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        var map = new MapTerm([new(Term.I(1), Term.Tuple(Term.I(9), Term.I(10)))]);
        nuint mapHandle = owner.Add(map);
        nuint borrowedKey = 0, borrowedValue = 0;
        foreach (nint hint in new nint[] { 0, unrelated.Pointer, nint.MaxValue })
        {
            NifNativeMapIterator504 iterator = default;
            Require(Create(
                table,
                hint,
                mapHandle,
                &iterator,
                NifNativeMapIterator504Names.First
            ) == 1);
            Require(Pair(
                table,
                hint,
                &iterator,
                &borrowedKey,
                &borrowedValue
            ) == 1);
            nuint firstKey = borrowedKey, firstValue = borrowedValue;
            Require(Pair(
                table,
                hint,
                &iterator,
                &borrowedKey,
                &borrowedValue
            ) == 1);
            Require(firstKey == borrowedKey && firstValue == borrowedValue);
            Require(owner.Get(borrowedValue).Equals(map.Get(Term.I(1))));
            Destroy(table, hint, &iterator);
            Require(owner.MapIteratorCount504 == 0);
            Require(owner.Get(borrowedValue).Equals(map.Get(Term.I(1))));
        }
        NifNativeMapIterator504 retained = default;
        Require(Create(
            table,
            0,
            mapHandle,
            &retained,
            NifNativeMapIterator504Names.First
        ) == 1);
        nuint copiedValue = unrelated.Add(owner.Get(borrowedValue));
        owner.ClearIndependent();
        Require(owner.MapIteratorCount504 == 0);
        RejectPair(table, &retained);
        Require(unrelated.Get(copiedValue).Equals(map.Get(Term.I(1))));
        failure.ThrowPending();
    }

    internal static void PendingPairs(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            failure
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint binary = 0;
        byte* bytes = ((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, byte*>)((nint*)table.Pointer)[NewBinarySlot])(owner.Pointer, 2, &binary);
        Require(bytes != null);
        bytes[0] = 1;
        bytes[1] = 2;
        nuint tuple = owner.AddValue(new NifNativeTermValue(new NifNativeTupleNode([owner.GetValue(binary)])));
        nuint map = ((delegate* unmanaged[Cdecl]<nint, nuint>)((nint*)table.Pointer)[NewMapSlot])(owner.Pointer);
        nuint pending = 0;
        Require(((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nuint*, int>)((nint*)table.Pointer)[PutSlot])(
            owner.Pointer,
            map,
            owner.Add(Term.I(1)),
            tuple,
            &pending
        ) == 1);
        NifNativeMapIterator504 iterator = default;
        Require(Create(
            table,
            owner.Pointer,
            pending,
            &iterator,
            NifNativeMapIterator504Names.First
        ) == 1);
        nuint key = 0, value = 0;
        Require(Pair(
            table,
            owner.Pointer,
            &iterator,
            &key,
            &value
        ) == 1);
        Require(owner.Get(value).Equals(Term.Tuple(new BitString([1, 2]))));
        bytes[0] = 3;
        bytes[1] = 4;
        Require(owner.Get(value).Equals(Term.Tuple(new BitString([3, 4]))));
        nuint nextKey = 0, nextValue = 0;
        Require(Pair(
            table,
            0,
            &iterator,
            &nextKey,
            &nextValue
        ) == 1 && key == nextKey && value == nextValue);
        Destroy(table, 0, &iterator);
        bytes[0] = 5;
        bytes[1] = 6;
        Require(owner.Get(value).Equals(Term.Tuple(new BitString([5, 6]))));
        failure.ThrowPending();
    }

    internal static void ContainmentAndThreads(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        var image = new ModuleCodeImage(Module);
        using var owner = new NifNativeCallEnvironment(
            context,
            image,
            0,
            failure
        );
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        var map = new MapTerm([new(Term.I(1), Term.I(2))]);
        nuint mapHandle = owner.Add(map), independentMap = independent.Add(map);
        NifNativeMapIterator504* processIterator = (NifNativeMapIterator504*)NativeMemory.Alloc((nuint)sizeof(NifNativeMapIterator504));
        NifNativeMapIterator504* independentIterator = (NifNativeMapIterator504*)NativeMemory.Alloc((nuint)sizeof(NifNativeMapIterator504));
        Thread? worker = null;
        try
        {
            Require(processIterator != null && independentIterator != null);
            Require(Create(
                table,
                0,
                mapHandle,
                processIterator,
                NifNativeMapIterator504Names.First
            ) == 1);
            Require(Create(
                table,
                0,
                independentMap,
                independentIterator,
                NifNativeMapIterator504Names.First
            ) == 1);
            Exception? workerFailure = null;
            worker = new Thread(() =>
            {
                try
                {
                    RejectPair(table, processIterator);
                    nuint key = 0, value = 0;
                    Require(Pair(
                        table,
                        0,
                        independentIterator,
                        &key,
                        &value
                    ) == 1);
                    Require(independent.Get(key).Equals(Term.I(1)) && independent.Get(value).Equals(Term.I(2)));
                }
                catch (Exception error)
                {
                    workerFailure = error;
                }
            });
            worker.Start();
            Require(worker.Join(Deadline));
            if (workerFailure is not null)
                throw new InvalidOperationException(Failure, workerFailure);
            Destroy(table, 0, processIterator);
            Destroy(table, 0, independentIterator);
            RejectPair(table, processIterator);
            Require(owner.MapIteratorCount504 == 0 && independent.MapIteratorCount504 == 0);
        }
        finally
        {
            // Both storage blocks remain owned until the finite callback body has actually exited.
            if (worker is { IsAlive: true })
                worker.Join();
            NativeMemory.Free(processIterator);
            NativeMemory.Free(independentIterator);
        }
        failure.ThrowPending();
    }

    private static bool ReadPair(
        NifNativeWindowsCallbacks table,
        NifNativeCallEnvironment owner,
        NifNativeMapIterator504* iterator,
        out KeyValuePair<Term, Term> pair
    )
    {
        nuint key = Unchanged, value = Unchanged;
        if (Pair(
            table,
            0,
            iterator,
            &key,
            &value
        ) == 0)
        {
            Require(key == Unchanged && value == Unchanged);
            pair = default;

            return false;
        }
        pair = new(owner.Get(key), owner.Get(value));

        return true;
    }

    private static void RejectPair(NifNativeWindowsCallbacks table, NifNativeMapIterator504* iterator)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        nuint key = Unchanged, value = Unchanged;
        Require(Pair(
            table,
            0,
            iterator,
            &key,
            &value
        ) == 0);
        Require(key == Unchanged && value == Unchanged && failure.HostFailure is not null && !failure.HasGuestException);
    }

    private static int Create(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint map,
        NifNativeMapIterator504* iterator,
        int entry
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, NifNativeMapIterator504*, int, int>)((nint*)table.Pointer)[CreateSlot])(
            env,
            map,
            iterator,
            entry
        );

    private static void Destroy(NifNativeWindowsCallbacks table, nint env, NifNativeMapIterator504* iterator)
        => ((delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, void>)((nint*)table.Pointer)[DestroySlot])(env, iterator);

    private static int Pair(
        NifNativeWindowsCallbacks table,
        nint env,
        NifNativeMapIterator504* iterator,
        nuint* key,
        nuint* value
    )
        => ((delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, nuint*, nuint*, int>)((nint*)table.Pointer)[PairSlot])(
            env,
            iterator,
            key,
            value
        );

    private static int Predicate(
        NifNativeWindowsCallbacks table,
        nint env,
        NifNativeMapIterator504* iterator,
        int slot
    )
        => ((delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, int>)((nint*)table.Pointer)[slot])(env, iterator);

    private static int Move(
        NifNativeWindowsCallbacks table,
        nint env,
        NifNativeMapIterator504* iterator,
        bool forward
    )
        => Predicate(
            table,
            env,
            iterator,
            forward ? NextSlot : PreviousSlot
        );

    private static void Fill(nuint* storage)
    {
        new Span<nuint>(storage, GuardedWords).Fill(Unchanged);
        storage[0] = Before;
        storage[GuardedWords - 1] = After;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
