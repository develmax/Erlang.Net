using System.Runtime.InteropServices;

namespace Erlang;

internal static class ErlDriverAsyncAccountingNative1126
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Row
    {
        internal uint Version, Identity, OperatingSystemThread, State;
        internal ulong Frequency, PerformanceCounter;
        internal ulong Aux, CheckIo, Emulator, Gc, Other, Port, Sleep;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Info(nint pool, out uint count, out ulong frequency);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int QpcInfo(nint pool, out ulong frequency);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Control(
        nint pool,
        uint action,
        uint identity,
        out Row row
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Finish(nint pool, uint action);
}
