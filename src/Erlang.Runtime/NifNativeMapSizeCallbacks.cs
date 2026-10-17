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
            if (owner.GetValue(term).StaticTerm is not MapTerm map)
                return 0;
            if (size == null)
                throw new ArgumentException(NifNativeMapSizeNames.NullOutput);
            *size = (nuint)map.Entries.Count;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
