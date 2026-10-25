using System.Numerics;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

internal static class NifNativeNumericHostChecks
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
        Term reason = await actor.Completion.WaitAsync(NifNativeNumericHostNames.Timeout);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        Require(NifNativeCallEnvironment.ActiveCount == active);
    }

    internal static unsafe void IntegerGetters(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        BigInteger[] inputs = [
            BigInteger.MinusOne, BigInteger.Zero, BigInteger.One,
            (BigInteger)int.MinValue - 1, int.MinValue, (BigInteger)int.MinValue + 1,
            (BigInteger)int.MaxValue - 1, int.MaxValue, (BigInteger)int.MaxValue + 1,
            (BigInteger)uint.MaxValue - 1, uint.MaxValue, (BigInteger)uint.MaxValue + 1,
            (BigInteger)long.MinValue - 1, long.MinValue, (BigInteger)long.MinValue + 1,
            (BigInteger)long.MaxValue - 1, long.MaxValue, (BigInteger)long.MaxValue + 1,
            (BigInteger)ulong.MaxValue - 1, ulong.MaxValue, (BigInteger)ulong.MaxValue + 1,
            BigInteger.One << 200, -(BigInteger.One << 200)
        ];
        foreach (int slot in NifNativeNumericHostNames.IntegerGetSlots)
        {
            (BigInteger minimum, BigInteger maximum) = Bounds(slot);
            foreach (BigInteger value in inputs)
                ProbeInteger(
                    table,
                    owner.Pointer,
                    owner.Add(new Integer(value)),
                    slot,
                    value >= minimum && value <= maximum ? value : null,
                    slot == NifNativeNumericHostNames.UInt64GetSlot && (value < minimum || value > maximum)
                        ? value.Sign < 0 ? NifNativeNumericHostNames.ExpectedUnsigned64BadArgument : NifNativeNumericHostNames.ExpectedUnsigned64SystemLimit
                        : null
                );
            foreach (Term value in WrongTypes())
                ProbeInteger(
                    table,
                    owner.Pointer,
                    owner.Add(value),
                    slot,
                    null,
                    slot == NifNativeNumericHostNames.UInt64GetSlot ? NifNativeNumericHostNames.ExpectedUnsigned64BadArgument : null
                );
        }
        // Existing int callbacks remain controls; no replacement or signature change.
        var slots = (nint*)table.Pointer;
        var existing = (delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)slots[NifNativeNumericHostNames.ExistingIntGetSlot];
        int result = 0;
        Require(existing(owner.Pointer, owner.Add(Term.I(int.MinValue)), &result) == 1 && result == int.MinValue);
        state.ThrowPending();
    }

    internal static unsafe void IntegerBuilders(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        foreach (int slot in NifNativeNumericHostNames.IntegerMakeSlots)
        {
            (BigInteger minimum, BigInteger maximum) = Bounds(MatchingGetter(slot));
            BigInteger[] values = [minimum, minimum + 1, BigInteger.Zero, BigInteger.One, maximum - 1, maximum];
            foreach (BigInteger value in values)
            {
                nuint token = MakeInteger(
                    table,
                    slot,
                    owner.Pointer,
                    value
                );
                Require(token != 0 && owner.Get(token) is Integer result && result.Value == value);
                ProbeInteger(
                    table,
                    owner.Pointer,
                    token,
                    MatchingGetter(slot),
                    value
                );
            }
        }
        var existing = (delegate* unmanaged[Cdecl]<nint, int, nuint>)((nint*)table.Pointer)[NifNativeNumericHostNames.ExistingIntMakeSlot];
        Require(owner.Get(existing(owner.Pointer, int.MaxValue)).Equals(Term.I(int.MaxValue)));
        state.ThrowPending();
    }

    internal static unsafe void Doubles(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var make = (delegate* unmanaged[Cdecl]<nint, double, nuint>)((nint*)table.Pointer)[NifNativeNumericHostNames.DoubleMakeSlot];
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, double*, int>)((nint*)table.Pointer)[NifNativeNumericHostNames.DoubleGetSlot];
        var state = new NifNativeFailureState();
        using (var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state))
        using (var owner = Environment(context, state))
        {
            double[] values = [0.0, -0.0, double.Epsilon, -double.Epsilon, double.MaxValue, double.MinValue, 0.5, -1.5];
            foreach (double value in values)
            {
                nuint token = make(owner.Pointer, value);
                Require(owner.Get(token) is FloatTerm result && BitConverter.DoubleToInt64Bits(result.Value) == BitConverter.DoubleToInt64Bits(value));
                double output = 19.25;
                Require(get(0, token, &output) == 1 && BitConverter.DoubleToInt64Bits(output) == BitConverter.DoubleToInt64Bits(value));
            }
            foreach (Term value in WrongTypes().Where(value => value is not FloatTerm).Append(Term.I(1)).Append(new Integer(BigInteger.One << 200)))
            {
                nuint token = owner.Add(value);
                double output = 19.25;
                Require(get(owner.Pointer, token, &output) == 0 && output == 19.25);
                Require(get(0, token, null) == 0);
            }
            state.ThrowPending();
        }
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            Require(make(owner.Pointer, invalid) == 0);
            Require(state.HostFailure is null && state.GuestPendingReason!.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            Require(owner.Get(owner.PendingReasonHandle).Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
    }

    internal static unsafe void EnvironmentAndContainment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var state = new NifNativeFailureState();
        using (var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state))
        using (var owner = Environment(context, state))
        using (var other = Environment(context, new NifNativeFailureState()))
        {
            nuint token = owner.Add(Term.I(1));
            foreach (int slot in NifNativeNumericHostNames.IntegerGetSlots)
            {
                ProbeInteger(
                    table,
                    0,
                    token,
                    slot,
                    BigInteger.One
                );
                ProbeInteger(
                    table,
                    other.Pointer,
                    token,
                    slot,
                    BigInteger.One
                );
                ProbeInteger(
                    table,
                    (nint)1,
                    token,
                    slot,
                    BigInteger.One
                );
                // Unlike other Windows getters, uint64 writes an error code even on refusal.
                if (slot != NifNativeNumericHostNames.UInt64GetSlot)
                    Require(GetInteger(
                        table,
                        slot,
                        0,
                        owner.Add(Nil.Value),
                        null
                    ) == 0);
            }
            state.ThrowPending();
        }
        foreach (int slot in NifNativeNumericHostNames.IntegerGetSlots.Append(NifNativeNumericHostNames.DoubleGetSlot))
        {
            state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            nuint token = owner.Add(slot == NifNativeNumericHostNames.DoubleGetSlot ? new FloatTerm(1.0) : Term.I(1));
            int result = slot == NifNativeNumericHostNames.DoubleGetSlot
                ? ((delegate* unmanaged[Cdecl]<nint, nuint, double*, int>)((nint*)table.Pointer)[slot])(0, token, null)
                : GetInteger(
                    table,
                    slot,
                    0,
                    token,
                    null
                );
            Require(result == 0 && state.HostFailure?.SourceException is InvalidOperationException && !state.HasGuestException);
        }
        foreach (int slot in NifNativeNumericHostNames.IntegerGetSlots.Append(NifNativeNumericHostNames.DoubleGetSlot))
        {
            state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            var expired = Environment(context, state);
            nuint token = expired.Add(Term.I(1));
            expired.Dispose();
            using var active = Environment(context, state);
            ulong output = ulong.MaxValue;
            int result = slot == NifNativeNumericHostNames.DoubleGetSlot
                ? ((delegate* unmanaged[Cdecl]<nint, nuint, double*, int>)((nint*)table.Pointer)[slot])(active.Pointer, token, (double*)&output)
                : GetInteger(
                    table,
                    slot,
                    active.Pointer,
                    token,
                    &output
                );
            Require(result == 0 && output == ulong.MaxValue);
            Require(state.HostFailure?.SourceException is InvalidOperationException && !state.HasGuestException);
        }
        foreach (int slot in NifNativeNumericHostNames.IntegerMakeSlots.Append(NifNativeNumericHostNames.DoubleMakeSlot))
        {
            state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            nuint token = slot == NifNativeNumericHostNames.DoubleMakeSlot
                ? ((delegate* unmanaged[Cdecl]<nint, double, nuint>)((nint*)table.Pointer)[slot])(0, 1.0)
                : MakeInteger(
                    table,
                    slot,
                    0,
                    BigInteger.One
                );
            Require(token == 0 && state.HostFailure?.SourceException is InvalidOperationException && !state.HasGuestException);
        }
    }

    private static unsafe void ProbeInteger(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint token,
        int slot,
        BigInteger? expected,
        ulong? refusalOutput = null
    )
    {
        byte* buffer = stackalloc byte[NifNativeNumericHostNames.BufferBytes];
        for (int index = 0; index < NifNativeNumericHostNames.BufferBytes; index++)
            buffer[index] = NifNativeNumericHostNames.Canary;
        byte* output = buffer + NifNativeNumericHostNames.GuardBytes;
        Require(GetInteger(
            table,
            slot,
            env,
            token,
            output
        ) == (expected.HasValue ? 1 : 0));
        int width = slot is NifNativeNumericHostNames.Int64GetSlot or NifNativeNumericHostNames.UInt64GetSlot ? sizeof(long) : sizeof(int);
        for (int index = 0; index < NifNativeNumericHostNames.BufferBytes; index++)
            if ((!expected.HasValue && !refusalOutput.HasValue) || index < NifNativeNumericHostNames.GuardBytes || index >= NifNativeNumericHostNames.GuardBytes + width)
                Require(buffer[index] == NifNativeNumericHostNames.Canary);
        if (refusalOutput.HasValue)
            Require(*(ulong*)output == refusalOutput.Value);
        if (expected.HasValue)
        {
            BigInteger actual = slot switch
            {
                NifNativeNumericHostNames.UnsignedLongGetSlot or NifNativeNumericHostNames.UnsignedIntGetSlot => *(uint*)output,
                NifNativeNumericHostNames.LongGetSlot => *(int*)output,
                NifNativeNumericHostNames.Int64GetSlot => *(long*)output,
                NifNativeNumericHostNames.UInt64GetSlot => *(ulong*)output,
                _ => throw new InvalidOperationException(NifNativeNumericHostNames.Failure)
            };
            Require(actual == expected.Value);
        }
    }

    private static unsafe int GetInteger(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        nuint token,
        void* output
    )
    {
        nint callback = ((nint*)table.Pointer)[slot];

        return slot switch
        {
            NifNativeNumericHostNames.UnsignedLongGetSlot or NifNativeNumericHostNames.UnsignedIntGetSlot => ((delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)callback)(env, token, (uint*)output),
            NifNativeNumericHostNames.LongGetSlot => ((delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)callback)(env, token, (int*)output),
            NifNativeNumericHostNames.Int64GetSlot => ((delegate* unmanaged[Cdecl]<nint, nuint, long*, int>)callback)(env, token, (long*)output),
            NifNativeNumericHostNames.UInt64GetSlot => ((delegate* unmanaged[Cdecl]<nint, nuint, ulong*, int>)callback)(env, token, (ulong*)output),
            _ => throw new InvalidOperationException(NifNativeNumericHostNames.Failure)
        };
    }

    private static unsafe nuint MakeInteger(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        BigInteger value
    )
    {
        nint callback = ((nint*)table.Pointer)[slot];

        return slot switch
        {
            NifNativeNumericHostNames.UnsignedLongMakeSlot or NifNativeNumericHostNames.UnsignedIntMakeSlot => ((delegate* unmanaged[Cdecl]<nint, uint, nuint>)callback)(env, (uint)value),
            NifNativeNumericHostNames.LongMakeSlot => ((delegate* unmanaged[Cdecl]<nint, int, nuint>)callback)(env, (int)value),
            NifNativeNumericHostNames.Int64MakeSlot => ((delegate* unmanaged[Cdecl]<nint, long, nuint>)callback)(env, (long)value),
            NifNativeNumericHostNames.UInt64MakeSlot => ((delegate* unmanaged[Cdecl]<nint, ulong, nuint>)callback)(env, (ulong)value),
            _ => throw new InvalidOperationException(NifNativeNumericHostNames.Failure)
        };
    }

    private static (BigInteger Minimum, BigInteger Maximum) Bounds(int slot)
        => slot switch
        {
            NifNativeNumericHostNames.UnsignedLongGetSlot or NifNativeNumericHostNames.UnsignedIntGetSlot => (uint.MinValue, uint.MaxValue),
            NifNativeNumericHostNames.LongGetSlot => (int.MinValue, int.MaxValue),
            NifNativeNumericHostNames.Int64GetSlot => (long.MinValue, long.MaxValue),
            NifNativeNumericHostNames.UInt64GetSlot => (ulong.MinValue, ulong.MaxValue),
            _ => throw new InvalidOperationException(NifNativeNumericHostNames.Failure)
        };

    private static int MatchingGetter(int slot)
        => slot switch
        {
            NifNativeNumericHostNames.UnsignedLongMakeSlot => NifNativeNumericHostNames.UnsignedLongGetSlot,
            NifNativeNumericHostNames.UnsignedIntMakeSlot => NifNativeNumericHostNames.UnsignedIntGetSlot,
            NifNativeNumericHostNames.LongMakeSlot => NifNativeNumericHostNames.LongGetSlot,
            NifNativeNumericHostNames.Int64MakeSlot => NifNativeNumericHostNames.Int64GetSlot,
            NifNativeNumericHostNames.UInt64MakeSlot => NifNativeNumericHostNames.UInt64GetSlot,
            _ => throw new InvalidOperationException(NifNativeNumericHostNames.Failure)
        };

    private static Term[] WrongTypes()
        => [Term.A(NifNativeNumericHostNames.Atom), new FloatTerm(1.0), Nil.Value,
            Term.List(Term.I(1)), Term.Tuple(Term.I(1)), new MapTerm([]), new BitString([])];

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeNumericHostNames.Module),
            0,
            state
        );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeNumericHostNames.Failure);
    }
}
