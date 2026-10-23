using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct WindowsPortProcessInformation
{
    public nint Process;
    public nint Thread;
    public int ProcessId;
    public int ThreadId;
}
