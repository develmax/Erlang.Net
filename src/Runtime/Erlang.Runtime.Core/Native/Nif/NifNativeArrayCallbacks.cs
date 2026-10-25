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
            NifNativeTermValue[]? deferred = null;
            for (int index = 0; index < items.Length; index++)
            {
                var value = environment.GetValue(values[index]);
                if (value.IsPending && deferred is null)
                {
                    deferred = new NifNativeTermValue[items.Length];
                    for (int previous = 0; previous < index; previous++)
                        deferred[previous] = new(items[previous]);
                }
                if (deferred is not null)
                    deferred[index] = value;
                else
                    items[index] = value.StaticTerm!;
            }
            if (deferred is null)
                return environment.Add(tuple ? Term.Tuple(items) : Term.List(items));
            if (tuple)
                return environment.AddValue(new NifNativeTermValue(new NifNativeTupleNode(deferred)));
            NifNativeTermValue result = new(Nil.Value);
            for (int index = deferred.Length - 1; index >= 0; index--)
            {
                result = deferred[index].IsPending || result.IsPending
                    ? new(new NifNativeConsNode(deferred[index], result))
                    : new(new Cons(deferred[index].StaticTerm!, result.StaticTerm!));
            }

            return environment.AddValue(result);
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
