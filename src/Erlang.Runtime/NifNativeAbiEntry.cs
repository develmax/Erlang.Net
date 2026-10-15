using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeAbiEntry
{
    public int Major;
    public int Minor;
    public nint Name;
    public int FunctionCount;
    public nint Functions;
    public nint Load;
    public nint Reload;
    public nint Upgrade;
    public nint Unload;
    public nint Variant;
    public uint Options;
    public nuint ResourceInitSize;
    public nint MinimumErts;
}
