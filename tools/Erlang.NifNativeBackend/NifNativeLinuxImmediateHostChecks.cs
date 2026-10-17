using System.Numerics;
using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeLinuxImmediateHostChecks
{
    internal static void Boundaries(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var invocation = scope.BindEnvironment(owner);
        NifNativeBridgeTable table = Table();
        Require((nint)table.MakeLong == (nint)table.MakeInt64);
        Require((nint)table.MakeUnsignedLong == (nint)table.MakeUInt64);
        foreach (long value in new[] { long.MinValue, NifNativeLinuxImmediateHostNames.Lower - 1,
            NifNativeLinuxImmediateHostNames.Lower, NifNativeLinuxImmediateHostNames.Lower + 1,
            -1, 0, 1, NifNativeLinuxImmediateHostNames.Upper - 1, NifNativeLinuxImmediateHostNames.Upper,
            NifNativeLinuxImmediateHostNames.Upper + 1, long.MaxValue })
        {
            Check(owner, table.MakeLong(owner.Pointer, value), value);
            Check(owner, table.MakeInt64(owner.Pointer, value), value);
            if (value >= NifNativeLinuxImmediateHostNames.Lower && value <= NifNativeLinuxImmediateHostNames.Upper)
            {
                Check(owner, table.MakeLong(0, value), value);
                Check(owner, table.MakeInt64(0, value), value);
            }
        }
        foreach (ulong value in new[] { 0UL, 1UL, (ulong)NifNativeLinuxImmediateHostNames.Upper - 1,
            (ulong)NifNativeLinuxImmediateHostNames.Upper, (ulong)NifNativeLinuxImmediateHostNames.Upper + 1, ulong.MaxValue })
        {
            Check(owner, table.MakeUnsignedLong(owner.Pointer, value), value);
            Check(owner, table.MakeUInt64(owner.Pointer, value), value);
            if (value <= (ulong)NifNativeLinuxImmediateHostNames.Upper)
            {
                Check(owner, table.MakeUnsignedLong(0, value), value);
                Check(owner, table.MakeUInt64(0, value), value);
            }
        }
        state.ThrowPending();
    }

    internal static void Ownership(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var other = Environment(context, state);
        using var invocation = scope.BindEnvironment(owner);
        NifNativeBridgeTable table = Table();
        foreach (nint hint in new[] { (nint)0, (nint)1, other.Pointer })
        {
            Check(owner, table.MakeLong(hint, NifNativeLinuxImmediateHostNames.Lower), NifNativeLinuxImmediateHostNames.Lower);
            Check(owner, table.MakeUInt64(hint, (ulong)NifNativeLinuxImmediateHostNames.Upper), NifNativeLinuxImmediateHostNames.Upper);
        }
        using (scope.BindEnvironment(other))
        {
            Check(other, table.MakeInt64(owner.Pointer, 1), 1);
            Check(other, table.MakeUnsignedLong(owner.Pointer, 1), 1);
        }
        Check(owner, table.MakeInt64(0, 1), 1);
        // A bignum is allocated in the valid supplied env, unlike an immediate.
        Check(other, table.MakeLong(other.Pointer, NifNativeLinuxImmediateHostNames.Lower - 1), NifNativeLinuxImmediateHostNames.Lower - 1);
        Check(other, table.MakeUInt64(other.Pointer, ulong.MaxValue), ulong.MaxValue);
        state.ThrowPending();
    }

    internal static void Containment(ProcessContext context)
    {
        NifNativeBridgeTable table = Table();
        for (int variant = 0; variant < 4; variant++)
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            using var invocation = scope.BindEnvironment(owner);
            nuint token = variant switch
            {
                0 => table.MakeLong(0, NifNativeLinuxImmediateHostNames.Lower - 1),
                1 => table.MakeInt64(0, NifNativeLinuxImmediateHostNames.Upper + 1),
                2 => table.MakeUnsignedLong(0, (ulong)NifNativeLinuxImmediateHostNames.Upper + 1),
                _ => table.MakeUInt64(0, ulong.MaxValue)
            };
            Require(token == 0 && state.HostFailure is not null && !state.HasGuestException);
        }
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            var expired = Environment(context, state);
            using var other = Environment(context, state);
            using var invocation = scope.BindEnvironment(expired);
            expired.Dispose();
            Require(table.MakeLong(other.Pointer, 1) == 0 && state.HostFailure is not null && !state.HasGuestException);
        }
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            Check(owner, table.MakeLong(owner.Pointer, 1), 1);
            Require(table.MakeUnsignedLong(0, 1) == 0 && state.HostFailure is not null && !state.HasGuestException);
        }
    }

    private static NifNativeBridgeTable Table() => (NifNativeBridgeTable)typeof(NifNativeExportRegistration)
        .GetMethod(NifNativeBridgeFixedHostNames.Factory, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeLinuxImmediateHostNames.Module),
            0,
            state
        );

    private static void Check(NifNativeCallEnvironment owner, nuint token, BigInteger expected)
        => Require(token != 0 && owner.Get(token) is Integer number && number.Value == expected);

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifNativeLinuxImmediateHostNames.Failure);
    }
}
