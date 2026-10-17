using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class NifNativeBinaryHostChecks
{
    internal static async Task Run(Action<ProcessContext> check)
    {
        int active = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int nativeOwned = NifNativeBinaryRegistry.NativeOwnedCount;
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        ProcessHandle actor = runtime.Spawn(context =>
        {
            try
            {
                check(context);
            }
            catch (Exception error)
            {
                failure = error;
            }

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        Term reason = await actor.Completion.WaitAsync(NifNativeBinaryHostNames.Timeout);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        Require(NifNativeCallEnvironment.ActiveCount == active);
        Require(NifNativeBinaryStorage.LiveBufferCount == buffers);
        Require(NifNativeBinaryRegistry.NativeOwnedCount == nativeOwned);
    }

    internal static unsafe void Inspection(ProcessContext context)
    {
        Require(Marshal.SizeOf<NifNativeBinaryDescriptor>() == 40);
        Require(Marshal.OffsetOf<NifNativeBinaryDescriptor>(nameof(NifNativeBinaryDescriptor.Size)) == 0);
        Require(Marshal.OffsetOf<NifNativeBinaryDescriptor>(nameof(NifNativeBinaryDescriptor.Data)) == 8);
        Require(Marshal.OffsetOf<NifNativeBinaryDescriptor>(nameof(NifNativeBinaryDescriptor.Reference)) == 16);
        Require(Marshal.OffsetOf<NifNativeBinaryDescriptor>(nameof(NifNativeBinaryDescriptor.Spare0)) == 24);
        Require(Marshal.OffsetOf<NifNativeBinaryDescriptor>(nameof(NifNativeBinaryDescriptor.Spare1)) == 32);
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        foreach (byte[] bytes in new byte[][] { [], [1], [0, 255, 127], Enumerable.Range(0, 8193).Select(value => (byte)value).ToArray() })
        {
            nuint term = env.Add(new BitString(bytes));
            var descriptor = Descriptor();
            Require(Inspect(
                table,
                env.Pointer,
                term,
                &descriptor
            ) == 1);
            Require(descriptor.Size == (nuint)bytes.Length && descriptor.Reference == 0 && descriptor.Data != null);
            Require(new ReadOnlySpan<byte>(descriptor.Data, bytes.Length).SequenceEqual(bytes));
            RequireSpares(descriptor);
            byte* pointer = descriptor.Data;
            Require(Inspect(
                table,
                0,
                term,
                &descriptor
            ) == 1 && descriptor.Data == pointer);
            Release(table, &descriptor); // Read-only descriptors need no release; this is a native no-op.
            Require(new ReadOnlySpan<byte>(pointer, bytes.Length).SequenceEqual(bytes));
        }
        foreach (Term value in new Term[] { Nil.Value, Term.I(1), new BitString(new byte[] { 128 }, 1), Term.Tuple() })
        {
            nuint term = env.Add(value);
            var descriptor = Descriptor();
            Require(Inspect(
                table,
                0,
                term,
                &descriptor
            ) == 0);
            RequireUnchanged(descriptor);
            Require(Inspect(
                table,
                0,
                term,
                null
            ) == 0);
        }
        state.ThrowPending();
    }

    internal static unsafe void Iolists(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var inspect = (delegate* unmanaged[Cdecl]<nint, nuint, NifNativeBinaryDescriptor*, int>)Slots(table)[NifNativeBinaryHostNames.InspectIolist];
        Term nested = new Cons(Term.List(Term.I(0), new BitString(new byte[] { 1, 2 })),
            new Cons(Term.List(Nil.Value, Term.I(255)), new BitString(new byte[] { 3 })));
        var descriptor = Descriptor();
        Require(inspect(env.Pointer, env.Add(nested), &descriptor) == 1);
        RequireBytes(descriptor, [0, 1, 2, 255, 3]);
        RequireSpares(descriptor);
        Require(inspect(0, env.Add(Nil.Value), &descriptor) == 1 && descriptor.Size == 0 && descriptor.Data != null && descriptor.Reference == 0);
        foreach (Term invalid in new Term[] { Term.I(1), Term.List(Term.I(256)), new Cons(Term.I(1), Term.I(2)),
            Term.List(new BitString(new byte[] { 128 }, 1)), Term.List(Term.A(NifNativeBinaryHostNames.Atom)) })
        {
            descriptor = Descriptor();
            Require(inspect(env.Pointer, env.Add(invalid), &descriptor) == 0);
            RequireUnchanged(descriptor);
        }
        // Flattening an iolist copies its current bytes; later valid writes to the
        // source new_binary affect the term graph, not the flattened temporary copy.
        nuint binary = 0;
        byte* data = New(
            table,
            env.Pointer,
            3,
            &binary
        );
        Require(data != null);
        Write(data, [1, 2, 3]);
        nuint list = Build(
            table,
            env.Pointer,
            false,
            env.Add(Term.I(9)),
            binary
        );
        Require(inspect(env.Pointer, list, &descriptor) == 1);
        RequireBytes(descriptor, [9, 1, 2, 3]);
        Write(data, [4, 5, 6]);
        RequireBytes(descriptor, [9, 1, 2, 3]);
        Require(((BitString)Cons.Items(env.Get(list)).Last()).ToArray().SequenceEqual(new byte[] { 4, 5, 6 }));
        state.ThrowPending();
    }

    internal static unsafe void AllocationAcrossCalls(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var descriptor = Descriptor();
        bool owned = false;
        try
        {
            var firstState = new NifNativeFailureState();
            var allocationOwner = new NifNativeAllocationOwner();
            using (var scope = new NifNativeCallbackScope(allocationOwner, firstState))
            using (var env = Environment(context, firstState))
            {
                Require(Allocate(table, 3, &descriptor) == 1);
                owned = true;
                Require(descriptor.Data != null && descriptor.Size == 3 && descriptor.Reference != 0);
                Write(descriptor.Data, [1, 2, 3]);
                RequireSpares(descriptor);
                firstState.ThrowPending();
            }
            allocationOwner.ReclaimAll(); // This raw enif_alloc owner must not own alloc_binary storage.
            RequireBytes(descriptor, [1, 2, 3]);
            var secondState = new NifNativeFailureState();
            Term returned;
            using (var scope = Scope(secondState))
            using (var env = Environment(context, secondState))
            {
                Require(Reallocate(table, &descriptor, 6) == 1 && descriptor.Size == 6);
                Require(new ReadOnlySpan<byte>(descriptor.Data, 3).SequenceEqual(new byte[] { 1, 2, 3 }));
                Write(descriptor.Data + 3, [4, 5, 6]);
                Require(Reallocate(table, &descriptor, 2) == 1);
                RequireBytes(descriptor, [1, 2]);
                nuint term = Make(table, env.Pointer, &descriptor);
                Require(term != 0 && descriptor.Reference == 0);
                owned = false;
                returned = env.Get(term);
                RequireBytes(descriptor, [1, 2]); // Transfer keeps data readable until this call ends.
                secondState.ThrowPending();
            }
            Require(((BitString)returned).ToArray().SequenceEqual(new byte[] { 1, 2 }));
        }
        finally
        {
            if (owned)
                Release(table, &descriptor);
        }
    }

    internal static unsafe void ReadonlyReallocation(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var descriptor = Descriptor();
        var original = new BitString(new byte[] { 7, 8, 9 });
        Require(Inspect(
            table,
            env.Pointer,
            env.Add(original),
            &descriptor
        ) == 1);
        var readonlyView = descriptor;
        bool owned = false;
        try
        {
            Require(Reallocate(table, &descriptor, 5) == 1);
            owned = true;
            Require(descriptor.Reference != 0 && descriptor.Data != readonlyView.Data);
            Require(new ReadOnlySpan<byte>(descriptor.Data, 3).SequenceEqual(new byte[] { 7, 8, 9 }));
            Write(descriptor.Data, [10, 11, 12, 13, 14]);
            RequireBytes(readonlyView, [7, 8, 9]);
            Require(original.ToArray().SequenceEqual(new byte[] { 7, 8, 9 }));
            nuint term = Make(table, env.Pointer, &descriptor);
            Require(term != 0 && descriptor.Reference == 0);
            owned = false;
            Require(((BitString)env.Get(term)).ToArray().SequenceEqual(new byte[] { 10, 11, 12, 13, 14 }));
            nuint copy = Make(table, env.Pointer, &readonlyView);
            Require(copy != 0 && ((BitString)env.Get(copy)).ToArray().SequenceEqual(original.ToArray()));
            RequireSpares(descriptor);
            state.ThrowPending();
        }
        finally
        {
            if (owned)
                Release(table, &descriptor);
        }
    }

    internal static unsafe void DeferredComposites(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        nuint binary = 0;
        byte* data = New(
            table,
            env.Pointer,
            4,
            &binary
        );
        Require(data != null && binary != 0);
        Write(data, [0, 0, 0, 0]);
        nuint tuple = Build(
            table,
            env.Pointer,
            true,
            binary,
            binary
        );
        nuint list = Build(
            table,
            env.Pointer,
            false,
            tuple,
            binary
        );
        nuint root = Build(
            table,
            env.Pointer,
            true,
            list,
            tuple
        );
        Require(Predicate(
            table,
            NifNativeBinaryHostNames.IsBinary,
            env.Pointer,
            binary
        ) == 1);
        Require(Predicate(
            table,
            NifNativeBinaryHostNames.IsTuple,
            env.Pointer,
            tuple
        ) == 1);
        Require(Predicate(
            table,
            NifNativeBinaryHostNames.IsList,
            env.Pointer,
            list
        ) == 1);
        var tupleGet = (delegate* unmanaged[Cdecl]<nint, nuint, int*, nuint**, int>)Slots(table)[NifNativeBinaryHostNames.TupleGet];
        int arity = 0;
        nuint* elements = null;
        Require(tupleGet(
            0,
            tuple,
            &arity,
            &elements
        ) == 1 && arity == 2);
        nuint fromTuple = elements[0];
        var cell = (delegate* unmanaged[Cdecl]<nint, nuint, nuint*, nuint*, int>)Slots(table)[NifNativeBinaryHostNames.ListCell];
        nuint head = 0, tail = 0;
        Require(cell(
            0,
            list,
            &head,
            &tail
        ) == 1);
        nuint fromList = Build(
            table,
            env.Pointer,
            true,
            head,
            tail,
            fromTuple
        );
        var length = (delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)Slots(table)[NifNativeBinaryHostNames.ListLength];
        uint count = 0;
        Require(length(0, list, &count) == 1 && count == 2);
        // Every composite and structural extraction above precedes the final writes.
        Write(data, [11, 22, 33, 44]);
        var result = (TupleTerm)env.Get(root);
        var pair = (TupleTerm)result.Items[1];
        Require(((BitString)pair.Items[0]).ToArray().SequenceEqual(new byte[] { 11, 22, 33, 44 }));
        Require(ReferenceEquals(pair.Items[0], pair.Items[1]));
        var extracted = (TupleTerm)env.Get(fromList);
        Require(((BitString)extracted.Items[2]).ToArray().SequenceEqual(new byte[] { 11, 22, 33, 44 }));
        Require(((BitString)((TupleTerm)Cons.Items(result.Items[0]).First()).Items[0]).ToArray().SequenceEqual(new byte[] { 11, 22, 33, 44 }));
        state.ThrowPending();
    }

    internal static unsafe void SubviewLifetimes(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var source = Environment(context, state);
        using var target = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        nuint binary = 0;
        byte* data = New(
            table,
            source.Pointer,
            96,
            &binary
        );
        Require(data != null);
        Write(data, new byte[96]);
        nuint shared = Sub(
            table,
            target.Pointer,
            binary,
            1,
            65
        );
        nuint sharedNested = Sub(
            table,
            target.Pointer,
            shared,
            0,
            65
        );
        nuint small63 = Sub(
            table,
            target.Pointer,
            binary,
            1,
            63
        );
        nuint small64 = Sub(
            table,
            target.Pointer,
            binary,
            1,
            64
        );
        nuint nested64 = Sub(
            table,
            target.Pointer,
            shared,
            1,
            64
        );
        nuint tuple = Build(
            table,
            target.Pointer,
            true,
            shared,
            sharedNested,
            small63,
            small64,
            nested64,
            shared
        );
        Write(data, Enumerable.Range(1, 96).Select(value => (byte)value).ToArray());
        source.Dispose();
        var result = (TupleTerm)target.Get(tuple);
        byte[] expected = Enumerable.Range(2, 65).Select(value => (byte)value).ToArray();
        Require(((BitString)result.Items[0]).ToArray().SequenceEqual(expected));
        Require(((BitString)result.Items[1]).ToArray().SequenceEqual(expected));
        Require(((BitString)result.Items[2]).ToArray().SequenceEqual(new byte[63]));
        Require(((BitString)result.Items[3]).ToArray().SequenceEqual(new byte[64]));
        Require(((BitString)result.Items[4]).ToArray().SequenceEqual(new byte[64]));
        Require(ReferenceEquals(result.Items[0], result.Items[5]));
        var bits = new BitString(new byte[] { 1, 2, 128 }, 17);
        nuint staticSub = Sub(
            table,
            target.Pointer,
            target.Add(bits),
            1,
            1
        );
        Require(((BitString)target.Get(staticSub)).ToArray().SequenceEqual(new byte[] { 2 }));
        nuint empty = Sub(
            table,
            target.Pointer,
            target.Add(bits),
            2,
            0
        );
        Require(((BitString)target.Get(empty)).ToArray().Length == 0);
        state.ThrowPending();
    }

    internal static unsafe void ExceptionalCleanup(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        long before = NifNativeBinaryStorage.LiveBufferCount;
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        Term immutable;
        using (var env = Environment(context, state))
        {
            nuint term = 0;
            byte* bytes = New(
                table,
                env.Pointer,
                3,
                &term
            );
            Require(bytes != null);
            Write(bytes, [12, 34, 56]);
            immutable = env.Get(Build(
                table,
                env.Pointer,
                true,
                term,
                term
            ));
        }
        Require(NifNativeBinaryStorage.LiveBufferCount == before);
        Require(((BitString)((TupleTerm)immutable).Items[0]).ToArray().SequenceEqual(new byte[] { 12, 34, 56 }));
        var failing = Environment(context, state);
        try
        {
            nuint term = 0;
            byte* bytes = New(
                table,
                failing.Pointer,
                16,
                &term
            );
            Require(bytes != null);
            Write(bytes, new byte[16]);
            var descriptor = Descriptor();
            Require(Inspect(
                table,
                failing.Pointer,
                failing.Add(new BitString(new byte[] { 1, 2 })),
                &descriptor
            ) == 1);
            throw new InvalidOperationException(NifNativeBinaryHostNames.ExpectedExit);
        }
        catch (InvalidOperationException error) when (error.Message == NifNativeBinaryHostNames.ExpectedExit)
        {
        }
        finally
        {
            failing.Dispose();
        }
        Require(NifNativeBinaryStorage.LiveBufferCount == before);
        state.ThrowPending();
    }

    internal static unsafe void ExceptionGraph(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        Term? escapedReason = null;
        using (var env = Environment(context, state))
        {
            nuint binary = 0;
            byte* bytes = New(
                table,
                env.Pointer,
                2,
                &binary
            );
            Require(bytes != null);
            Write(bytes, [0, 0]);
            nuint reason = Build(
                table,
                env.Pointer,
                true,
                binary,
                binary
            );
            var raise = (delegate* unmanaged[Cdecl]<nint, nuint, nuint>)Slots(table)[NifNativeBinaryHostNames.RaiseException];
            var pending = (delegate* unmanaged[Cdecl]<nint, nuint*, int>)Slots(table)[NifNativeBinaryHostNames.PendingException];
            var isException = (delegate* unmanaged[Cdecl]<nint, nuint, int>)Slots(table)[NifNativeBinaryHostNames.IsException];
            Require(raise(env.Pointer, reason) == 0);
            nuint queried = 0;
            Require(state.HasGuestException && pending(env.Pointer, &queried) == 1 && queried == reason);
            Require(isException(env.Pointer, 0) == 1);
            Write(bytes, [21, 43]);
            try
            {
                state.ThrowPending();
            }
            catch (ErlangException error)
            {
                escapedReason = error.Reason;
            }
            Require(escapedReason is TupleTerm);
        }
        var tuple = (TupleTerm)escapedReason!;
        Require(((BitString)tuple.Items[0]).ToArray().SequenceEqual(new byte[] { 21, 43 }));
        Require(ReferenceEquals(tuple.Items[0], tuple.Items[1]));
        Require(state.GuestPendingReason!.Equals(escapedReason));
        // A later callback may replace a pending reason; the query must not return
        // a cached earlier graph handle. The tuple guard refuses before reading
        // its null array and performs no enormous allocation.
        var replacementState = new NifNativeFailureState();
        using var replacementScope = Scope(replacementState);
        using var replacementEnv = Environment(context, replacementState);
        nuint replacementBinary = 0;
        byte* replacementBytes = New(
            table,
            replacementEnv.Pointer,
            1,
            &replacementBinary
        );
        Require(replacementBytes != null);
        *replacementBytes = 0;
        nuint previousReason = Build(
            table,
            replacementEnv.Pointer,
            true,
            replacementBinary
        );
        var raiseReplacement = (delegate* unmanaged[Cdecl]<nint, nuint, nuint>)Slots(table)[NifNativeBinaryHostNames.RaiseException];
        var refusedTuple = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)Slots(table)[NifNativeBinaryHostNames.Tuple];
        var queryReplacement = (delegate* unmanaged[Cdecl]<nint, nuint*, int>)Slots(table)[NifNativeBinaryHostNames.PendingException];
        Require(raiseReplacement(replacementEnv.Pointer, previousReason) == 0);
        Require(refusedTuple(replacementEnv.Pointer, null, uint.MaxValue) == 0);
        nuint replaced = 0;
        Require(queryReplacement(replacementEnv.Pointer, &replaced) == 1);
        Require(replacementEnv.Get(replaced).Equals(Term.A(ErlangErrorReasons.SystemLimit)));
        Require(replacementState.HostFailure is null);
    }

    internal static unsafe void DeepGraph(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        nuint term = 0;
        byte* bytes = New(
            table,
            env.Pointer,
            1,
            &term
        );
        Require(bytes != null);
        *bytes = 0;
        for (int index = 0; index < NifNativeBinaryHostNames.DeepLevels; index++)
            term = Build(
                table,
                env.Pointer,
                true,
                term
            );
        *bytes = 77;
        Term result = env.Get(term);
        for (int index = 0; index < NifNativeBinaryHostNames.DeepLevels; index++)
        {
            Require(result is TupleTerm tuple && tuple.Items.Count == 1);
            result = ((TupleTerm)result).Items[0];
        }
        Require(((BitString)result).ToArray().SequenceEqual(new byte[] { 77 }));
        state.ThrowPending();
    }

    internal static unsafe void Containment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var state = new NifNativeFailureState();
        using (var scope = Scope(state))
        using (var env = Environment(context, state))
        {
            nuint empty = 123;
            byte* data = New(
                table,
                env.Pointer,
                0,
                &empty
            );
            Require(data != null && empty != 123 && env.Get(empty) is BitString binary && binary.BitLength == 0);
            var descriptor = Descriptor();
            Require(Allocate(table, 0, &descriptor) == 1 && descriptor.Data != null && descriptor.Reference != 0);
            Release(table, &descriptor);
            state.ThrowPending();
        }
        for (int operation = 0; operation < 5; operation++)
        {
            state = new NifNativeFailureState();
            using var scope = Scope(state);
            using var env = Environment(context, state);
            switch (operation)
            {
                case 0:
                    Require(Allocate(table, 1, null) == 0);
                    break;
                case 1:
                    Require(New(
                        table,
                        env.Pointer,
                        1,
                        null
                    ) == null);
                    break;
                case 2:
                    Require(Inspect(
                        table,
                        0,
                        env.Add(new BitString(new byte[] { 1 })),
                        null
                    ) == 0);
                    break;
                case 3:
                    Require(Sub(
                        table,
                        env.Pointer,
                        env.Add(new BitString(new byte[] { 1 })),
                        1,
                        1
                    ) == 0);
                    break;
                case 4:
                    var expired = Environment(context, state);
                    nuint term = expired.Add(new BitString(new byte[] { 1 }));
                    expired.Dispose();
                    var output = Descriptor();
                    Require(Inspect(
                        table,
                        env.Pointer,
                        term,
                        &output
                    ) == 0);
                    RequireUnchanged(output);
                    break;
            }
            Require(state.HostFailure is not null && !state.HasGuestException);
        }
    }

    private static unsafe nuint Build(
        NifNativeWindowsCallbacks table,
        nint env,
        bool tuple,
        params nuint[] values
    )
    {
        var make = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)Slots(table)[tuple ? NifNativeBinaryHostNames.Tuple : NifNativeBinaryHostNames.List];
        fixed (nuint* items = values)
            return make(env, items, (uint)values.Length);
    }

    private static unsafe int Inspect(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint term,
        NifNativeBinaryDescriptor* output
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, NifNativeBinaryDescriptor*, int>)Slots(table)[NifNativeBinaryHostNames.Inspect])(env, term, output);

    private static unsafe int Allocate(NifNativeWindowsCallbacks table, nuint size, NifNativeBinaryDescriptor* output)
        => ((delegate* unmanaged[Cdecl]<nuint, NifNativeBinaryDescriptor*, int>)Slots(table)[NifNativeBinaryHostNames.Allocate])(size, output);

    private static unsafe int Reallocate(NifNativeWindowsCallbacks table, NifNativeBinaryDescriptor* output, nuint size)
        => ((delegate* unmanaged[Cdecl]<NifNativeBinaryDescriptor*, nuint, int>)Slots(table)[NifNativeBinaryHostNames.Reallocate])(output, size);

    private static unsafe void Release(NifNativeWindowsCallbacks table, NifNativeBinaryDescriptor* output)
        => ((delegate* unmanaged[Cdecl]<NifNativeBinaryDescriptor*, void>)Slots(table)[NifNativeBinaryHostNames.Release])(output);

    private static unsafe nuint Make(NifNativeWindowsCallbacks table, nint env, NifNativeBinaryDescriptor* output)
        => ((delegate* unmanaged[Cdecl]<nint, NifNativeBinaryDescriptor*, nuint>)Slots(table)[NifNativeBinaryHostNames.Make])(env, output);

    private static unsafe byte* New(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint size,
        nuint* term
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, byte*>)Slots(table)[NifNativeBinaryHostNames.New])(env, size, term);

    private static unsafe nuint Sub(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint term,
        nuint position,
        nuint size
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nuint>)Slots(table)[NifNativeBinaryHostNames.Sub])(
            env,
            term,
            position,
            size
        );

    private static unsafe int Predicate(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        nuint value
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, int>)Slots(table)[slot])(env, value);

    private static unsafe nint* Slots(NifNativeWindowsCallbacks table) => (nint*)table.Pointer;

    private static unsafe NifNativeBinaryDescriptor Descriptor() => new()
    {
        Size = 99,
        Data = null,
        Reference = 0,
        Spare0 = NifNativeBinaryHostNames.Spare0,
        Spare1 = NifNativeBinaryHostNames.Spare1
    };

    private static unsafe void RequireUnchanged(NifNativeBinaryDescriptor value)
        => Require(value.Size == 99 && value.Data == null && value.Reference == 0 && value.Spare0 == NifNativeBinaryHostNames.Spare0 && value.Spare1 == NifNativeBinaryHostNames.Spare1);

    private static void RequireSpares(NifNativeBinaryDescriptor value)
        => Require(value.Spare0 == NifNativeBinaryHostNames.Spare0 && value.Spare1 == NifNativeBinaryHostNames.Spare1);

    private static unsafe void RequireBytes(NifNativeBinaryDescriptor descriptor, byte[] expected)
        => Require(descriptor.Size == (nuint)expected.Length && descriptor.Data != null && new ReadOnlySpan<byte>(descriptor.Data, expected.Length).SequenceEqual(expected));

    private static unsafe void Write(byte* output, byte[] bytes) => bytes.CopyTo(new Span<byte>(output, bytes.Length));

    private static NifNativeCallbackScope Scope(NifNativeFailureState state) => new(new NifNativeAllocationOwner(), state);

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeBinaryHostNames.Module),
            0,
            state
        );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeBinaryHostNames.Failure);
    }
}
