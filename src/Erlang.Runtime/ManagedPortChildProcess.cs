using System.Diagnostics;

namespace Erlang;

internal sealed class ManagedPortChildProcess(Process process) : PortChildProcess
{
    public override int Id => process.Id;
    public override int ExitCode => process.ExitCode;
    public override Stream Input => process.StandardInput.BaseStream;
    public override Stream Output => process.StandardOutput.BaseStream;

    public static PortChildProcess StartManaged(PortOpenOptions options)
    {
        var process = new Process { StartInfo = options.Start };
        try
        {
            process.Start();

            return new ManagedPortChildProcess(process);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    public override Task WaitForExitAsync(CancellationToken cancellation) => process.WaitForExitAsync(cancellation);

    public override void Dispose() => process.Dispose();
}
