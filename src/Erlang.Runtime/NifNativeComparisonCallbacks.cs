using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeComparisonCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Identical(nuint left, nuint right)
    {
        try
        {
            return Read(left).Equals(Read(right))
                ? NifNativeComparisonNames.Identical : NifNativeComparisonNames.Different;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeComparisonNames.FailureResult;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Compare(nuint left, nuint right)
    {
        try
        {
            int result = TermOrder.Compare(Read(left), Read(right));

            return result < 0 ? NifNativeComparisonNames.Before
                : result > 0 ? NifNativeComparisonNames.After : NifNativeComparisonNames.SameOrder;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeComparisonNames.FailureResult;
        }
    }

    // Each read resolves the current pending graph. It never replaces a node or caches its bytes.
    private static Term Read(nuint handle) => NifNativeCallEnvironment.ResolveTermOwner(handle, 0).Get(handle);
}
