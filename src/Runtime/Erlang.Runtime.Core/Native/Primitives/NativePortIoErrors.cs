namespace Erlang;

internal static class NativePortIoErrors
{
    public static string Reason(Exception exception) => exception switch
    {
        NativePortIOException { Error: NativePortIoParameters.BadDescriptor } => PortNames.BadDescriptor,
        NativePortIOException { Error: NativePortIoParameters.BrokenPipe } => PortNames.BrokenPipe,
        IOException io when OperatingSystem.IsWindows() && (io.HResult & NativePortIoParameters.WindowsErrorMask) == NativePortIoParameters.WindowsBrokenPipe => PortNames.BrokenPipe,
        _ => PortNames.IoError
    };
}
