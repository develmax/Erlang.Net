namespace Erlang;

internal abstract class PortChildProcess : IDisposable
{
    public virtual bool HasChild => true;
    public abstract int Id { get; }
    public abstract int ExitCode { get; }
    public abstract Stream Input { get; }
    public abstract Stream Output { get; }

    public abstract Task WaitForExitAsync(CancellationToken cancellation);

    public abstract void Dispose();

    public static PortChildProcess Start(PortOpenOptions options)
    {
        if (options.IsDescriptor)
            return DescriptorPortResource.Open(options);
        if (!options.NativeRequired)
            return ManagedPortChildProcess.StartManaged(options);
        if (OperatingSystem.IsWindows())
            return WindowsPortChildProcess.StartWindows(options);
        if (OperatingSystem.IsLinux())
            return LinuxPortChildProcess.StartLinux(options);

        throw new ErlangException(PortNames.NotSupported);
    }
}
