using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeListCellCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Get(
        nint env,
        nuint value,
        nuint* head,
        nuint* tail
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(value, env);
            // Pinned OTP returns before touching output pointers for every non-cons.
            if (!owner.GetValue(value).TryCons(out var headValue, out var tailValue))
                return 0;
            if (head == null || tail == null)
                throw new InvalidOperationException(NifNativeListCellNames.NullOutput);
            nuint headHandle = owner.AddValue(headValue);
            nuint tailHandle = owner.AddValue(tailValue);
            *head = headHandle;
            *tail = tailHandle;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Create(nint env, nuint head, nuint tail)
    {
        NifNativeCallEnvironment? environment = null;
        try
        {
            environment = NifNativeCallEnvironment.Resolve(env);
            NifNativeTermValue headValue = environment.GetValue(head);
            NifNativeTermValue tailValue = environment.GetValue(tail);
            NifNativeTermValue result = headValue.IsPending || tailValue.IsPending
                ? new(new NifNativeConsNode(headValue, tailValue))
                : new(new Cons(headValue.StaticTerm!, tailValue.StaticTerm!));

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
