using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeArrayCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeTuple(nint env, nuint* values, uint count)
        => Make(
            env,
            values,
            count,
            tuple: true
        );

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeList(nint env, nuint* values, uint count)
        => Make(
            env,
            values,
            count,
            tuple: false
        );

    private static nuint Make(
        nint env,
        nuint* values,
        uint count,
        bool tuple
    )
    {
        NifNativeCallEnvironment? environment = null;
        try
        {
            environment = NifNativeCallEnvironment.Resolve(env);
            uint maximum = tuple ? (uint)ErlangTupleLimits.MaximumArity : (uint)Array.MaxLength;
            if (count > maximum)
            {
                environment.Failure.Raise(NifNativeArrayNames.SystemLimit);

                return 0;
            }
            // Pinned OTP accepts a null array for zero elements and never dereferences it.
            if (count != 0 && values == null)
                throw new InvalidOperationException(NifNativeArrayNames.NullArray);
            Term[] items = count == 0 ? [] : new Term[checked((int)count)];
            for (int index = 0; index < items.Length; index++)
                items[index] = environment.Get(values[index]);

            return environment.Add(tuple ? Term.Tuple(items) : Term.List(items));
        }
        catch (OutOfMemoryException) when (environment is not null)
        {
            environment.Failure.Raise(NifNativeArrayNames.SystemLimit);

            return 0;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
