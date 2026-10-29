using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeAbiEntryPrefix651
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
}
