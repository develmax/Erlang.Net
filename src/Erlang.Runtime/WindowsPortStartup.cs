using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct WindowsPortStartup
{
    public int Size;
    public nint Reserved;
    public nint Desktop;
    public nint Title;
    public uint X;
    public uint Y;
    public uint XSize;
    public uint YSize;
    public uint XCharacters;
    public uint YCharacters;
    public uint FillAttribute;
    public uint Flags;
    public ushort ShowWindow;
    public ushort ReservedSize;
    public nint ReservedBytes;
    public nint Input;
    public nint Output;
    public nint Error;
    public nint Attributes;
}
