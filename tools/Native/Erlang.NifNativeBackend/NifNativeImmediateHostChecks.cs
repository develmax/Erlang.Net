namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeImmediateHostChecks
{
    internal static void Getter(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var other = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)Slots(table)[NifNativeImmediateHostNames.GetInt];
        foreach (int value in new[] { int.MinValue, 0, 1, int.MaxValue })
        {
            nuint token = owner.Add(Term.I(value));
            foreach (nint hint in new[] { (nint)0, (nint)1, other.Pointer })
            {
                int result = 42;
                Require(get(hint, token, &result) == 1 && result == value);
            }
        }
        foreach (Term value in new Term[] {
            Nil.Value, Term.A(NifNativeImmediateHostNames.WrongType), new FloatTerm(1.0),
            Term.Tuple(Term.I(1)), Term.I((long)int.MaxValue + 1), Term.I((long)int.MinValue - 1)
        })
        {
            nuint token = owner.Add(value);
            int result = 42;
            Require(get(0, token, null) == 0);
            Require(get(other.Pointer, token, &result) == 0 && result == 42);
        }
        state.ThrowPending();
    }

    internal static void Ownership(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var other = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        using var invocation = scope.BindEnvironment(owner);
        foreach (int slot in NifNativeImmediateHostNames.ImmediateSlots)
        {
            long[] values = slot is NifNativeImmediateHostNames.MakeUnsignedLong or NifNativeImmediateHostNames.MakeUnsignedInt
                ? [0, 1, uint.MaxValue] : [int.MinValue, 0, 1, int.MaxValue];
            foreach (long value in values)
            {
                foreach (nint hint in new[] { (nint)0, (nint)1, other.Pointer })
                    Check(owner, Make(
                        table,
                        slot,
                        hint,
                        value
                    ), value);
                using (scope.BindEnvironment(other))
                    Check(other, Make(
                        table,
                        slot,
                        owner.Pointer,
                        value
                    ), value);
                Check(owner, Make(
                    table,
                    slot,
                    0,
                    value
                ), value);
            }
        }
        state.ThrowPending();
    }

    internal static void Containment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        foreach (int slot in NifNativeImmediateHostNames.ImmediateSlots)
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            // Old managed HOST controls retain their explicit valid-env route when unbound.
            Check(owner, Make(
                table,
                slot,
                owner.Pointer,
                1
            ), 1);
            Require(Make(
                table,
                slot,
                0,
                1
            ) == 0 && state.HostFailure is not null && !state.HasGuestException);
        }
        foreach (int slot in NifNativeImmediateHostNames.ImmediateSlots)
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            var expired = Environment(context, state);
            using var other = Environment(context, state);
            using var invocation = scope.BindEnvironment(expired);
            expired.Dispose();
            Require(Make(
                table,
                slot,
                other.Pointer,
                1
            ) == 0);
            Require(state.HostFailure is not null && !state.HasGuestException);
        }
        foreach (int slot in new[] { NifNativeImmediateHostNames.MakeInt64, NifNativeImmediateHostNames.MakeUInt64 })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            using var invocation = scope.BindEnvironment(owner);
            nuint result = slot == NifNativeImmediateHostNames.MakeInt64
                ? ((delegate* unmanaged[Cdecl]<nint, long, nuint>)Slots(table)[slot])(0, 1)
                : ((delegate* unmanaged[Cdecl]<nint, ulong, nuint>)Slots(table)[slot])(0, 1);
            // Windows real64 constructors need env even for small results; Linux aliases differ.
            Require(result == 0 && state.HostFailure is not null && !state.HasGuestException);
        }
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            var get = (delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)Slots(table)[NifNativeImmediateHostNames.GetInt];
            Require(get(0, owner.Add(Term.I(1)), null) == 0 && state.HostFailure is not null && !state.HasGuestException);
        }
    }

    private static nuint Make(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        long value
    )
        => slot is NifNativeImmediateHostNames.MakeUnsignedLong or NifNativeImmediateHostNames.MakeUnsignedInt
            ? ((delegate* unmanaged[Cdecl]<nint, uint, nuint>)Slots(table)[slot])(env, (uint)value)
            : ((delegate* unmanaged[Cdecl]<nint, int, nuint>)Slots(table)[slot])(env, (int)value);

    private static void Check(NifNativeCallEnvironment owner, nuint token, long value)
        => Require(token != 0 && owner.Get(token) is Integer number && number.Value == value);

    private static nint* Slots(NifNativeWindowsCallbacks table) => (nint*)table.Pointer;

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeImmediateHostNames.Module),
            0,
            state
        );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifNativeImmediateHostNames.Failure);
    }
}
