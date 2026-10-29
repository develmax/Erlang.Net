using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeEnvironmentCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Free(nint env)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            if (!owner.IsIndependent)
                throw new InvalidOperationException(NifNativeEnvironmentNames.ProcessBound);
            owner.Dispose();
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Clear(nint env)
    {
        try
        {
            NifNativeCallEnvironment.Resolve(env).ClearIndependent();
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Copy(nint destination, nuint source)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(destination);
            var original = NifNativeCallEnvironment.ResolveTermOwner(source, destination);
            if (!ReferenceEquals(owner.Runtime, original.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);

            return owner.CopyValueFromOwnedSource683(original.GetValue(source), original.UsesOwnedExecution683);
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeReferenceNames.FailedHandle;
        }
    }
}
