using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeSequenceCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetTuple(
        nint env,
        nuint value,
        int* arity,
        nuint** elements
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(value, env);
            var tuple = owner.GetValue(value);
            if (!tuple.TryTupleArity(out int count))
                return 0;
            if (arity == null || elements == null)
                throw new InvalidOperationException(NifNativeTextNames.NullOutput);
            nuint* array = owner.BorrowTupleElements(tuple);
            *arity = count;
            *elements = array;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetListLength(nint env, nuint value, uint* length)
    {
        try
        {
            var current = NifNativeCallEnvironment.ResolveTermOwner(value, env).GetValue(value);
            uint count = 0;
            while (current.TryCons(out _, out var tail))
            {
                if (count == uint.MaxValue)
                    return 0;
                count++;
                current = tail;
            }
            if (!current.IsNil)
                return 0;
            if (length == null)
                throw new InvalidOperationException(NifNativeTextNames.NullOutput);
            *length = count;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
