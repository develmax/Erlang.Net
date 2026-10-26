using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeMapSizeCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Size(nint env, nuint term, nuint* size)
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(term, env);
            var value = owner.GetValue(term);
            int count = value.StaticTerm is MapTerm map ? map.Entries.Count
                : value.Pending is NifNativeMapNode504 pending ? pending.Entries.Count : -1;
            if (count < 0)
                return 0;
            if (size == null)
                throw new ArgumentException(NifNativeMapSizeNames.NullOutput);
            *size = (nuint)count;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
