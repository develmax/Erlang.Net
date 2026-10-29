using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class ReMagicRegistry597
{
    private static readonly object Gate = new();
    private static readonly Dictionary<nint, ReMagicBacking597> Live = new();
    internal static nint Destructor => (nint)(delegate* unmanaged[Cdecl]<nint, int>)&Destroy;

    internal static void Add(nint address, ReMagicBacking597 backing)
    {
        lock (Gate)
            Live.Add(address, backing);
    }

    internal static void Remove(nint address, ReMagicBacking597 backing)
    {
        lock (Gate)
        {
            if (!Live.TryGetValue(address, out var actual) || !ReferenceEquals(actual, backing))
                throw new InvalidOperationException(ReMagicNames597.Invalid);
            Live.Remove(address);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Destroy(nint address)
    {
        ReMagicBacking597? backing = null;
        try
        {
            lock (Gate) Live.TryGetValue(address, out backing);

            // No foreign raw address is dereferenced before exact admission.
            return backing?.InvokeActualCodeDestructor(address) ?? 0;
        }
        catch (Exception error)
        {
            backing?.RecordDestructorFailure597(error);

            return 0; // Never throw across reverse P/Invoke.
        }
    }
}
