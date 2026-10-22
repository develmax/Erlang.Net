using System.Runtime.InteropServices;

namespace Erlang.HaltProbe;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct HaltWindowsDumpProcessEntry437
{
    public uint Size;
    public uint Usage;
    public uint ProcessId;
    public nuint DefaultHeapId;
    public uint ModuleId;
    public uint Threads;
    public uint ParentProcessId;
    public int PriorityClassBase;
    public uint Flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HaltWindowsDumpNames437.MaxPath)]
    public string Executable;
}
