using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class ErlDriverHostPipeIdentity420
{
    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "GetNamedPipeClientProcessId", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint process);

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "GetNamedPipeServerProcessId", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint process);

    internal static void RequireChild(NamedPipeServerStream pipe, int process)
    {
        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint actual))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        if (actual != checked((uint)process)) throw new InvalidDataException(ErlDriverHostNames420.WrongPeer);
    }

    internal static void RequireParent(NamedPipeClientStream pipe, int process)
    {
        if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out uint actual))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        if (actual != checked((uint)process)) throw new InvalidDataException(ErlDriverHostNames420.WrongPeer);
    }
}
