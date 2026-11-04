using System.Runtime.InteropServices;

namespace Erlang;

internal static class LinuxDescriptorPollNative
{
    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    internal static extern int poll([In, Out] DescriptorPollState[] descriptors, nuint count, int timeout);

    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    internal static extern int close(int descriptor);
}
