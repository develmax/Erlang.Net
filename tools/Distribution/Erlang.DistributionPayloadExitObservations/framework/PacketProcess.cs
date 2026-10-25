using System.Diagnostics;
using System.Globalization;

namespace DistributionPacketContrasts;

internal static class PacketProcess
{
    internal static async Task<PacketProcessResult> Run(string executable, string[] arguments, string prefix)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException(PacketDiagnostics.ProcessStart);
        using var captureStop = new CancellationTokenSource();
        await using var stdoutFile = File.Create(prefix + PacketCaptureProtocol.StdoutSuffix);
        await using var stderrFile = File.Create(prefix + PacketCaptureProtocol.StderrSuffix);
        Task stdout = process.StandardOutput.BaseStream.CopyToAsync(stdoutFile, captureStop.Token);
        Task stderr = process.StandardError.BaseStream.CopyToAsync(stderrFile, captureStop.Token);
        bool timedOut = false;
        bool rawComplete = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(PacketNames.TimeoutSeconds));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
            }
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(PacketNames.KilledProcessWaitSeconds));
        }
        finally
        {
            if (process.HasExited)
                await File.WriteAllTextAsync(
                    prefix + PacketCaptureProtocol.ExitSuffix,
                    process.ExitCode.ToString(CultureInfo.InvariantCulture) + PacketCaptureProtocol.NewLine
                );
            if (!process.HasExited)
                captureStop.Cancel();
            try
            {
                await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(PacketNames.StreamDrainSeconds));
                rawComplete = true;
            }
            catch (TimeoutException)
            {
                await File.WriteAllTextAsync(prefix + PacketCaptureProtocol.CompleteSuffix, PacketCaptureProtocol.CompleteFalse);
                captureStop.Cancel();
                process.StandardOutput.Close();
                process.StandardError.Close();
                try
                {
                    await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(PacketNames.CaptureCancellationJoinSeconds));
                }
                catch (OperationCanceledException) when (captureStop.IsCancellationRequested)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                catch (IOException) when (captureStop.IsCancellationRequested)
                {
                }
                catch (TimeoutException)
                {
                    _ = Task.WhenAll(stdout, stderr).ContinueWith(
                        completed => _ = completed.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default
                    );
                }
            }
            catch (OperationCanceledException) when (captureStop.IsCancellationRequested)
            {
            }
            await stdoutFile.FlushAsync();
            await stderrFile.FlushAsync();
            await File.WriteAllTextAsync(
                prefix + PacketCaptureProtocol.CompleteSuffix,
                rawComplete ? PacketCaptureProtocol.CompleteTrue : PacketCaptureProtocol.CompleteFalse
            );
        }

        return new(
            process.ExitCode,
            timedOut,
            rawComplete
        );
    }
}
