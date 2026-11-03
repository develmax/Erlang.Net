using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct NativeResourceCallbacks527
{
    internal readonly nint Destructor;
    internal readonly nint Stop;
    internal readonly nint Down;
    internal readonly int Members;
    internal readonly nint DynamicCall;

    internal NativeResourceCallbacks527(
        nint destructor,
        nint stop = 0,
        nint down = 0,
        nint dynamicCall = 0
    )
    {
        Destructor = destructor;
        Stop = stop;
        Down = down;
        Members = 4;
        DynamicCall = dynamicCall;
    }

    internal bool HasCallbacks527 => Destructor != 0 || Stop != 0 || Down != 0 || DynamicCall != 0;

    internal static unsafe NativeResourceCallbacks527 Read527(nint pointer, int count)
    {
        // Pinned release builds fall through the switch default after zeroing
        // the destination; DEBUG builds assert. This profile admits release x64.
        if (count is < 0 or > 4) return default;
        if (count == 0) return default;
        if (pointer == 0) throw new ArgumentNullException(nameof(pointer));
        // Read only admitted members; legacy open_x never inspects members or
        // dyncall beyond its first three pointers.
        var source = (NativeResourceCallbacks527*)pointer;

        return new(
            source->Destructor,
            count >= 2 ? source->Stop : 0,
            count >= 3 ? source->Down : 0,
            count >= 4 ? source->DynamicCall : 0
        );
    }
}
