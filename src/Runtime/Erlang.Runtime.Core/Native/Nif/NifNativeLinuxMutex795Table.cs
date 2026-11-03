using System.Runtime.InteropServices;
namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeLinuxMutex795Table
{
    internal uint Version;
    internal uint Size;
    internal nint Create;
    internal nint Destroy;
    internal nint TryLock;
    internal nint Lock;
    internal nint Unlock;
    internal nint Name;
}
