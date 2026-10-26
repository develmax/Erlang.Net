using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeMapCreation501Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Make(nint env)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);

            return owner.Add(new MapTerm([]));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeMapCreation501Names.FailedHandle;
        }
    }
}
