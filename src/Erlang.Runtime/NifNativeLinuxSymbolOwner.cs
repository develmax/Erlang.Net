using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeLinuxSymbolOwner
{
    internal nint Path;
    internal nint ImageBase;
    internal nint SymbolName;
    internal nint SymbolAddress;
}
