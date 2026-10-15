using System.Runtime.InteropServices;

namespace NifPrototype;

[StructLayout(LayoutKind.Sequential)]
internal struct NifAbiEntry
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
