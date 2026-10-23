using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal sealed class DescriptorPortResource(Stream input, Stream output) : PortChildProcess
{
    public override bool HasChild => false;
    public override int Id => -1;
    public override int ExitCode => 0;
    public override Stream Input => input;
    public override Stream Output => output;

    public override Task WaitForExitAsync(CancellationToken cancellation) => Task.CompletedTask;

    public override void Dispose()
    {
        input.Dispose();
        output.Dispose();
    }

    public static DescriptorPortResource Open(PortOpenOptions options)
    {
        Stream? input = null;
        try
        {
            input = options.Write ? OpenStream(options.OutputDescriptor, false) : Stream.Null;
            Stream output = options.Read ? OpenStream(options.InputDescriptor, true) : Stream.Null;

            return new(input, output);
        }
        catch
        {
            input?.Dispose();
            throw;
        }
    }

    private static Stream OpenStream(int descriptor, bool readable)
    {
        if (OperatingSystem.IsLinux())
        {
            if (descriptor < 0 || LinuxPortNative.fcntl(descriptor, LinuxPortParameters.GetFlags, 0) < 0)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return new DescriptorPortStream(descriptor, readable);
        }
        if (OperatingSystem.IsWindows())
        {
            nint handle = descriptor switch
            {
                0 => WindowsPortNative.GetStdHandle(DescriptorPortParameters.WindowsInput),
                1 => WindowsPortNative.GetStdHandle(DescriptorPortParameters.WindowsOutput),
                2 => WindowsPortNative.GetStdHandle(WindowsPortParameters.StandardError),
                _ => descriptor
            };

            return new WindowsDescriptorPortStream(new SafeFileHandle(handle, ownsHandle: false), readable);
        }

        throw new ErlangException(PortNames.NotSupported);
    }
}
