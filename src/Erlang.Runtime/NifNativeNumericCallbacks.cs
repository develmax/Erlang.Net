using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeNumericCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetUnsignedLong(
        nint env,
        nuint term,
        uint* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            if (owner.Get(term) is not Integer number ||
                number.Value < uint.MinValue || number.Value > uint.MaxValue)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeNumericNames.NullOutput);
            *output = (uint)number.Value;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetDouble(
        nint env,
        nuint term,
        double* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            if (owner.Get(term) is not FloatTerm number)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeNumericNames.NullOutput);
            *output = (double)number.Value;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeUnsignedLong(nint env, uint value)
    {
        try
        {
            var owner = NifNativeImmediateOwner.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeDouble(nint env, double value)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            if (!double.IsFinite(value))
            {
                owner.Raise(owner.Add(NifNativeNames.BadArgument));

                return 0;
            }

            return owner.Add(new FloatTerm(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetUnsignedInt(
        nint env,
        nuint term,
        uint* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            if (owner.Get(term) is not Integer number ||
                number.Value < uint.MinValue || number.Value > uint.MaxValue)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeNumericNames.NullOutput);
            *output = (uint)number.Value;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetLong(
        nint env,
        nuint term,
        int* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            if (owner.Get(term) is not Integer number ||
                number.Value < int.MinValue || number.Value > int.MaxValue)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeNumericNames.NullOutput);
            *output = (int)number.Value;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeUnsignedInt(nint env, uint value)
    {
        try
        {
            var owner = NifNativeImmediateOwner.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeLong(nint env, int value)
    {
        try
        {
            var owner = NifNativeImmediateOwner.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetInt64(
        nint env,
        nuint term,
        long* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            if (owner.Get(term) is not Integer number ||
                number.Value < long.MinValue || number.Value > long.MaxValue)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeNumericNames.NullOutput);
            *output = (long)number.Value;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetUInt64(
        nint env,
        nuint term,
        ulong* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            if (output == null)
                throw new InvalidOperationException(NifNativeNumericNames.NullOutput);
            // On x64 OTP's term_to_Uint64 delegates to term_to_Uint, which writes
            // its internal error code to the output even though conversion returns zero.
            if (owner.Get(term) is not Integer number || number.Value.Sign < 0)
            {
                *output = NifNativeNumericConversionCodes.BadArgument;

                return 0;
            }
            if (number.Value > ulong.MaxValue)
            {
                *output = NifNativeNumericConversionCodes.SystemLimit;

                return 0;
            }
            *output = (ulong)number.Value;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeInt64(nint env, long value)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeUInt64(nint env, ulong value)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
