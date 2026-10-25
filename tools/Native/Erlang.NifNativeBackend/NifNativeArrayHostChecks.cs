using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

internal static class NifNativeArrayHostChecks
{
    internal static async Task Run(Action<ProcessContext> check)
    {
        int active = NifNativeCallEnvironment.ActiveCount;
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
        Term reason = await actor.Completion.WaitAsync(NifNativeArrayHostNames.Timeout);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        Require(NifNativeCallEnvironment.ActiveCount == active);
    }

    internal static unsafe void Empty(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var environment = new NifNativeCallEnvironment(
            context,
            Image(),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        var slots = (nint*)table.Pointer;
        var tuple = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[NifNativeArrayHostNames.TupleSlot];
        var list = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[NifNativeArrayHostNames.ListSlot];
        Require(tuple != null && list != null);
        Require(environment.Get(tuple(environment.Pointer, null, 0)).Equals(Term.Tuple()));
        Require(environment.Get(list(environment.Pointer, null, 0)) is Nil);
        state.ThrowPending();
    }

    internal static unsafe void Order(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var environment = new NifNativeCallEnvironment(
            context,
            Image(),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        var slots = (nint*)table.Pointer;
        var tuple = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[NifNativeArrayHostNames.TupleSlot];
        var list = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[NifNativeArrayHostNames.ListSlot];
        Term payload = Term.Tuple(Term.I(NifNativeArrayHostNames.PayloadInteger), Term.List(Nil.Value));
        Term[] expected = [payload, Nil.Value, payload];
        nuint* handles = stackalloc nuint[3];
        handles[0] = environment.Add(payload);
        handles[1] = environment.Add(Nil.Value);
        handles[2] = handles[0];
        Term actualTuple = environment.Get(tuple(environment.Pointer, handles, 3));
        Term actualList = environment.Get(list(environment.Pointer, handles, 3));
        handles[0] = handles[1];
        Require(actualTuple.Equals(Term.Tuple(expected)));
        Require(actualList.Equals(Term.List(expected)));
        var many = new nuint[NifNativeArrayHostNames.ManyItems];
        Array.Fill(many, handles[2]);
        fixed (nuint* values = many)
        {
            var largeTuple = (TupleTerm)environment.Get(tuple(environment.Pointer, values, (uint)many.Length));
            Term largeList = environment.Get(list(environment.Pointer, values, (uint)many.Length));
            Require(largeTuple.Items.Count == many.Length && largeTuple.Items.All(item => ReferenceEquals(item, payload)));
            Require(Cons.Items(largeList).Count() == many.Length && Cons.Items(largeList).All(item => ReferenceEquals(item, payload)));
        }
        state.ThrowPending();
    }

    internal static unsafe void Limits(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var slots = (nint*)table.Pointer;
        foreach (int slot in new[] { NifNativeArrayHostNames.TupleSlot, NifNativeArrayHostNames.ListSlot })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var environment = new NifNativeCallEnvironment(
                context,
                Image(),
                0,
                state
            );
            var callback = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[slot];
            // Deliberately invalid count is rejected before any array read or allocation.
            Require(callback(environment.Pointer, null, uint.MaxValue) == 0);
            Require(state.HasGuestException && state.GuestPendingReason!.Equals(Term.A(ErlangErrorReasons.SystemLimit)));
            Require(state.HostFailure is null);
            try
            {
                state.ThrowPending();
                throw new InvalidOperationException(NifNativeArrayHostNames.Failure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)))
            {
            }
        }
    }

    internal static unsafe void Containment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var slots = (nint*)table.Pointer;
        foreach (int slot in new[] { NifNativeArrayHostNames.TupleSlot, NifNativeArrayHostNames.ListSlot })
        {
            var state = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state))
            using (var environment = new NifNativeCallEnvironment(
                context,
                Image(),
                0,
                state
            ))
            {
                var callback = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[slot];
                Require(callback(environment.Pointer, null, 1) == 0);
                Require(state.HostFailure?.SourceException is InvalidOperationException error && error.Message == NifNativeArrayNames.NullArray);
            }
            state = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state))
            using (var environment = new NifNativeCallEnvironment(
                context,
                Image(),
                0,
                state
            ))
            using (var foreign = new NifNativeCallEnvironment(
                context,
                Image(),
                0,
                new NifNativeFailureState()
            ))
            {
                nuint token = foreign.Add(Nil.Value);
                var callback = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[slot];
                Require(callback(environment.Pointer, &token, 1) == 0);
                Require(state.HostFailure?.SourceException is InvalidOperationException error && error.Message == NifNativeDiagnostics.ForeignTermHandle);
            }
            state = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state))
            {
                var expired = new NifNativeCallEnvironment(
                    context,
                    Image(),
                    0,
                    state
                );
                nint pointer = expired.Pointer;
                expired.Dispose();
                var callback = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[slot];
                Require(callback(pointer, null, 0) == 0);
                Require(state.HostFailure?.SourceException is InvalidOperationException error && error.Message == NifNativeDiagnostics.ExpiredEnvironment);
            }
        }
    }

    private static ModuleCodeImage Image() => new(NifNativeArrayHostNames.Module);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeArrayHostNames.Failure);
    }
}
