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
            if (owner.Get(value) is not Cons cell)
                return 0;
            if (head == null || tail == null)
                throw new InvalidOperationException(NifNativeListCellNames.NullOutput);
            nuint headValue = owner.Add(cell.Head);
            nuint tailValue = owner.Add(cell.Tail);
            *head = headValue;
            *tail = tailValue;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
