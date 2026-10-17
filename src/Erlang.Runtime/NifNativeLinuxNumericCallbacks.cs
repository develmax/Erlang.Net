using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeLinuxNumericCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeLong(nint env, long value)
    {
        try
        {
            var owner = value >= NifNativeLinuxNumericNames.MinimumSmall && value <= NifNativeLinuxNumericNames.MaximumSmall
                ? NifNativeImmediateOwner.Resolve(env) : NifNativeCallEnvironment.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeUnsignedLong(nint env, ulong value)
    {
        try
        {
            var owner = value <= (ulong)NifNativeLinuxNumericNames.MaximumSmall
                ? NifNativeImmediateOwner.Resolve(env) : NifNativeCallEnvironment.Resolve(env);

            return owner.Add(new Integer(value));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
