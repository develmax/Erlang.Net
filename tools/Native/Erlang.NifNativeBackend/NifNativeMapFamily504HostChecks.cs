using static Erlang.NifNativeBackend.NifNativeMapFamily504HostNames;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeMapFamily504HostChecks
{
    internal static void MutationAndOutput(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint* words = stackalloc nuint[3];
        nuint value = owner.Add(Term.I(Replacement));
        foreach (int count in new[] { 0, 1, FlatMaximum, FlatMaximum + 1, 65 })
        {
            var original = Map(count);
            nuint map = owner.Add(original);
            nuint present = owner.Add(Term.I(count == 0 ? 1 : count));
            nuint missing = owner.Add(Term.I(count + 1));
            Reset(words);
            Require(Change(
                table,
                Put,
                owner.Pointer,
                map,
                missing,
                value,
                &words[1]
            ) == 1);
            Edges(words);
            Require(MapSize(table, owner.Pointer, words[1]) == count + 1);
            Require(GetValue(
                table,
                owner,
                words[1],
                missing
            ).Equals(Term.I(Replacement)));
            Require(MapSize(table, owner.Pointer, map) == count && original.Entries.Count == count);
            Reset(words);
            Require(Change(
                table,
                Update,
                owner.Pointer,
                map,
                missing,
                value,
                &words[1]
            ) == 0);
            Edges(words);
            Require(words[1] == (count > FlatMaximum ? 0 : Unchanged));
            Reset(words);
            Require(Change(
                table,
                Remove,
                owner.Pointer,
                map,
                missing,
                0,
                &words[1]
            ) == 1);
            Edges(words);
            Require(MapSize(table, owner.Pointer, words[1]) == count);
            if (count == 0)
                continue;
            Reset(words);
            Require(Change(
                table,
                Update,
                owner.Pointer,
                map,
                present,
                value,
                &words[1]
            ) == 1);
            Edges(words);
            Require(MapSize(table, owner.Pointer, words[1]) == count);
            Require(GetValue(
                table,
                owner,
                words[1],
                present
            ).Equals(Term.I(Replacement)));
            Require(GetValue(
                table,
                owner,
                map,
                present
            ).Equals(Term.I(count * 10)));
            Reset(words);
            Require(Change(
                table,
                Remove,
                owner.Pointer,
                map,
                present,
                0,
                &words[1]
            ) == 1);
            Edges(words);
            Require(MapSize(table, owner.Pointer, words[1]) == count - 1);
            nuint output = Unchanged;
            Require(Get(
                table,
                owner.Pointer,
                words[1],
                present,
                &output
            ) == 0 && output == Unchanged);
        }
        foreach (Term nonmap in new Term[] { Nil.Value, Term.I(1), new FloatTerm(1.0), Term.Tuple(Term.I(1)), Term.List(Term.I(1)), new BitString([]) })
        {
            nuint map = owner.Add(nonmap);
            foreach (int slot in new[] { Put, Update, Remove })
            {
                Reset(words);
                Require(Change(
                    table,
                    slot,
                    0,
                    map,
                    nuint.MaxValue,
                    nuint.MaxValue,
                    &words[1]
                ) == 0);
                Require(words[1] == Unchanged);
                Require(Change(
                    table,
                    slot,
                    0,
                    map,
                    nuint.MaxValue,
                    nuint.MaxValue,
                    null
                ) == 0);
            }
        }
        state.ThrowPending();
    }

    internal static void ArraysAndAliases(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint* words = stackalloc nuint[3];
        Reset(words);
        Require(FromArrays(
            table,
            owner.Pointer,
            null,
            null,
            0,
            &words[1]
        ) == 1);
        Edges(words);
        Require(MapSize(table, owner.Pointer, words[1]) == 0);
        nuint* keys = stackalloc nuint[3];
        nuint* values = stackalloc nuint[3];
        keys[0] = owner.Add(Term.I(1));
        keys[1] = owner.Add(new FloatTerm(1.0));
        values[0] = owner.Add(Term.I(11));
        values[1] = owner.Add(Term.I(22));
        Reset(words);
        Require(FromArrays(
            table,
            owner.Pointer,
            keys,
            values,
            2,
            &words[1]
        ) == 1);
        Edges(words);
        Require(MapSize(table, owner.Pointer, words[1]) == 2);
        Require(GetValue(
            table,
            owner,
            words[1],
            keys[0]
        ).Equals(Term.I(11)));
        Require(GetValue(
            table,
            owner,
            words[1],
            keys[1]
        ).Equals(Term.I(22)));
        keys[2] = owner.Add(Term.I(1));
        values[2] = owner.Add(Term.I(33));
        Reset(words);
        Require(FromArrays(
            table,
            owner.Pointer,
            keys,
            values,
            3,
            &words[1]
        ) == 0);
        Edges(words);
        Require(words[1] == 0);
        nuint initial = owner.Add(new MapTerm([]));
        nuint alias = initial;
        Require(Change(
            table,
            Put,
            owner.Pointer,
            alias,
            keys[0],
            values[0],
            &alias
        ) == 1);
        Require(MapSize(table, owner.Pointer, alias) == 1 && MapSize(table, owner.Pointer, initial) == 0);
        Require(Change(
            table,
            Update,
            owner.Pointer,
            alias,
            keys[0],
            values[1],
            &alias
        ) == 1);
        Require(GetValue(
            table,
            owner,
            alias,
            keys[0]
        ).Equals(Term.I(22)));
        Require(Change(
            table,
            Remove,
            owner.Pointer,
            alias,
            keys[0],
            0,
            &alias
        ) == 1);
        Require(MapSize(table, owner.Pointer, alias) == 0);
        state.ThrowPending();
    }

    internal static void PendingValuesAndReplacement(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint key = owner.Add(Term.I(1));
        nuint* keys = stackalloc nuint[1];
        nuint* values = stackalloc nuint[1];
        foreach (int slot in new[] { Put, Update, Arrays })
        {
            nuint binary = 0;
            byte* bytes = New(
                table,
                owner.Pointer,
                2,
                &binary
            );
            bytes[0] = 1;
            bytes[1] = 2;
            nuint map = owner.Add(new MapTerm([new(Term.I(1), Term.I(0))]));
            if (slot == Arrays)
            {
                keys[0] = key;
                values[0] = binary;
                Require(FromArrays(
                    table,
                    owner.Pointer,
                    keys,
                    values,
                    1,
                    &map
                ) == 1);
            }
            else
                Require(Change(
                    table,
                    slot,
                    owner.Pointer,
                    map,
                    key,
                    binary,
                    &map
                ) == 1);
            nuint borrowed = 0;
            Require(Get(
                table,
                0,
                map,
                key,
                &borrowed
            ) == 1);
            nuint wrapped = owner.AddValue(new NifNativeTermValue(new NifNativeTupleNode([owner.GetValue(borrowed)])));
            foreach (byte changed in new byte[] { 3, 5, 7 })
            {
                bytes[0] = changed;
                bytes[1] = (byte)(changed + 1);
                Term expected = new BitString(new byte[] { changed, (byte)(changed + 1) });
                Require(owner.Get(borrowed).Equals(expected));
                Require(GetValue(
                    table,
                    owner,
                    map,
                    key
                ).Equals(expected));
                Require(owner.Get(wrapped).Equals(Term.Tuple(expected)));
            }
        }
        nuint argument = 0;
        byte* argumentBytes = New(
            table,
            owner.Pointer,
            2,
            &argument
        );
        argumentBytes[0] = 1;
        argumentBytes[1] = 2;
        nuint original = owner.Add(new MapTerm([new(new BitString(new byte[] { 1, 2 }), Term.I(11))]));
        nuint replaced = 0;
        Require(Change(
            table,
            Put,
            owner.Pointer,
            original,
            argument,
            owner.Add(Term.I(22)),
            &replaced
        ) == 1);
        argumentBytes[0] = 3;
        argumentBytes[1] = 4;
        Require(MapSize(table, 0, replaced) == 1);
        nuint oldKey = owner.Add(new BitString(new byte[] { 1, 2 }));
        Require(GetValue(
            table,
            owner,
            replaced,
            oldKey
        ).Equals(Term.I(22)));
        nuint missing = Unchanged;
        Require(Get(
            table,
            0,
            replaced,
            argument,
            &missing
        ) == 0 && missing == Unchanged);
        Require(GetValue(
            table,
            owner,
            original,
            oldKey
        ).Equals(Term.I(11)));
        state.ThrowPending();
    }

    internal static void CopyBoundariesAndOwnerLifetime(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        var image = new ModuleCodeImage(Module);
        using var owner = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var other = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        table.BindIndependentEnvironments(context.Runtime, owner.Image);
        nuint key = owner.Add(Term.I(1));
        foreach (int size in new[] { 63, 64, 65, 4096 })
        {
            nuint binary = 0;
            byte* bytes = New(
                table,
                owner.Pointer,
                (nuint)size,
                &binary
            );
            new Span<byte>(bytes, size).Fill(InitialByte);
            nuint map = owner.Add(new MapTerm([]));
            Require(Change(
                table,
                Put,
                owner.Pointer,
                map,
                key,
                binary,
                &map
            ) == 1);
            nuint copied = CopyValue(table, other.Pointer, map);
            Require(copied != 0);
            nuint returned = CopyValue(table, owner.Pointer, copied);
            Require(returned != 0);
            other.ClearIndependent();
            new Span<byte>(bytes, size).Fill(ChangedByte);
            BitString live = (BitString)GetValue(
                table,
                owner,
                map,
                key
            );
            BitString snapshot = (BitString)GetValue(
                table,
                owner,
                returned,
                key
            );
            Require(live.Equals(new BitString(Enumerable.Repeat(ChangedByte, size).ToArray())));
            byte expected = size <= 64 ? InitialByte : ChangedByte;
            Require(snapshot.Equals(new BitString(Enumerable.Repeat(expected, size).ToArray())));
        }
        nuint independentMap = CopyValue(table, other.Pointer, owner.Add(new MapTerm([])));
        nuint independentKey = other.Add(Term.I(1));
        nuint independentValue = other.Add(Term.Tuple(Term.I(2)));
        Require(Change(
            table,
            Put,
            other.Pointer,
            independentMap,
            independentKey,
            independentValue,
            &independentMap
        ) == 1);
        nuint surviving = CopyValue(table, owner.Pointer, independentMap);
        other.ClearIndependent();
        Require(GetValue(
            table,
            owner,
            surviving,
            key
        ).Equals(Term.Tuple(Term.I(2))));
        state.ThrowPending();
    }

    internal static void InvalidOwnersAndThreads(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        var image = new ModuleCodeImage(Module);
        using var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        nuint map = caller.Add(new MapTerm([]));
        nuint key = caller.Add(Term.I(1));
        nuint value = caller.Add(Term.I(2));
        RejectMutation(
            table,
            Put,
            0,
            map,
            key,
            value,
            false
        );
        RejectMutation(
            table,
            Put,
            independent.Pointer,
            map,
            key,
            value,
            false
        );
        RejectMutation(
            table,
            Put,
            caller.Pointer,
            map,
            key,
            value,
            true
        );
        RejectMutation(
            table,
            Put,
            caller.Pointer,
            nuint.MaxValue,
            key,
            value,
            false
        );
        RejectArrays(
            table,
            caller.Pointer,
            key,
            value,
            true,
            false
        );
        RejectArrays(
            table,
            caller.Pointer,
            key,
            value,
            false,
            true
        );
        var foreign = new ProcessRuntime();
        try
        {
            using var foreignOwner = new NifNativeCallEnvironment(foreign, image);
            RejectMutation(
                table,
                Put,
                caller.Pointer,
                map,
                foreignOwner.Add(Term.I(1)),
                value,
                false
            );
            RejectMutation(
                table,
                Put,
                caller.Pointer,
                map,
                key,
                foreignOwner.Add(Term.I(2)),
                false
            );
        }
        finally
        {
            foreign.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        nuint workerMap = independent.Add(new MapTerm([]));
        nuint workerKey = independent.Add(Term.I(1));
        nuint workerValue = independent.Add(Term.I(2));
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try
            {
                RejectMutation(
                    table,
                    Put,
                    caller.Pointer,
                    map,
                    key,
                    value,
                    false
                );
                var workerState = new NifNativeFailureState();
                using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), workerState);
                nuint output = 0;
                Require(Change(
                    table,
                    Put,
                    independent.Pointer,
                    workerMap,
                    workerKey,
                    workerValue,
                    &output
                ) == 1);
                Require(GetValue(
                    table,
                    independent,
                    output,
                    workerKey
                ).Equals(Term.I(2)));
                workerState.ThrowPending();
            }
            catch (Exception failure)
            {
                workerFailure = failure;
            }
        })
        { IsBackground = true };
        worker.Start();
        Require(worker.Join(Deadline));
        if (workerFailure is not null)
            throw new InvalidOperationException(Failure, workerFailure);
        independent.ClearIndependent();
        RejectMutation(
            table,
            Put,
            independent.Pointer,
            workerMap,
            workerKey,
            workerValue,
            false
        );
        state.ThrowPending();
    }

    private static void RejectMutation(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        nuint map,
        nuint key,
        nuint value,
        bool nullOutput
    )
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        nuint output = Unchanged;
        Require(Change(
            table,
            slot,
            env,
            map,
            key,
            value,
            nullOutput ? null : &output
        ) == 0);
        Require(output == Unchanged && failure.HostFailure is not null && !failure.HasGuestException);
    }

    private static void RejectArrays(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint key,
        nuint value,
        bool nullInputs,
        bool nullOutput
    )
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        nuint output = Unchanged;
        Require(FromArrays(
            table,
            env,
            nullInputs ? null : &key,
            nullInputs ? null : &value,
            1,
            nullOutput ? null : &output
        ) == 0);
        Require(output == Unchanged && failure.HostFailure is not null && !failure.HasGuestException);
    }

    private static MapTerm Map(int count)
        => new(Enumerable.Range(1, count).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index * 10))));

    private static int Change(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        nuint map,
        nuint key,
        nuint value,
        nuint* output
    )
    {
        nint callback = Slot(table, slot);

        return slot == Remove
            ? ((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)callback)(
                env,
                map,
                key,
                output
            )
            : ((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nuint*, int>)callback)(
                env,
                map,
                key,
                value,
                output
            );
    }

    private static int FromArrays(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint* keys,
        nuint* values,
        nuint count,
        nuint* output
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint*, nuint*, nuint, nuint*, int>)Slot(table, Arrays))(
            env,
            keys,
            values,
            count,
            output
        );

    private static int Get(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint map,
        nuint key,
        nuint* output
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)Slot(table, NifNativeMapFamily504HostNames.Get))(
            env,
            map,
            key,
            output
        );

    private static Term GetValue(
        NifNativeWindowsCallbacks table,
        NifNativeCallEnvironment owner,
        nuint map,
        nuint key
    )
    {
        nuint output = 0;
        Require(Get(
            table,
            0,
            map,
            key,
            &output
        ) == 1);

        return owner.Get(output);
    }

    private static int MapSize(NifNativeWindowsCallbacks table, nint env, nuint map)
    {
        nuint count = nuint.MaxValue;
        Require(((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, int>)Slot(table, Size))(env, map, &count) == 1);

        return checked((int)count);
    }

    private static byte* New(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint count,
        nuint* output
    )
    {
        byte* bytes = ((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, byte*>)Slot(table, NewBinary))(env, count, output);
        Require(bytes != null && *output != 0);

        return bytes;
    }

    private static nuint CopyValue(NifNativeWindowsCallbacks table, nint env, nuint value)
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint>)Slot(table, Copy))(env, value);

    private static nint Slot(NifNativeWindowsCallbacks table, int index)
    {
        nint callback = ((nint*)table.Pointer)[index];
        Require(callback != 0);

        return callback;
    }

    private static void Reset(nuint* output)
    {
        output[0] = Before;
        output[1] = Unchanged;
        output[2] = After;
    }

    private static void Edges(nuint* output) => Require(output[0] == Before && output[2] == After);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
