using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct WindowsPortSecurity
{
    public int Length;
    public nint Descriptor;
    [MarshalAs(UnmanagedType.Bool)] public bool Inherit;
}
