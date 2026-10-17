using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeReferenceCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Make(nint env)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);

            return owner.Add(owner.Runtime.MakeReference());
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeReferenceNames.FailedHandle;
        }
    }
}
