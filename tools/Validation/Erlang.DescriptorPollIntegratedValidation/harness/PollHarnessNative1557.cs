using System.Runtime.InteropServices;

namespace Erlang;

internal static class PollHarnessNative1557
{
    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    internal static extern int dup3(int source, int destination, int flags);
}
