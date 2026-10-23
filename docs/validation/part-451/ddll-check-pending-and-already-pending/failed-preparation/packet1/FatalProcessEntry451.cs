using System.Runtime.InteropServices;



[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct FatalProcessEntry451
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
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string Executable;
}
