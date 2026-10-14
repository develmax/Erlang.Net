using System.Diagnostics;
using System.Globalization;

namespace NodeIdentityContrasts;

internal static class NodeProcess
{
    internal static async Task<NodeProcessResult> Run(string executable, string[] arguments, string prefix)
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
        using var process = Process.Start(start) ?? throw new IOException(NodeDiagnostics.ProcessStart);
        using var captureStop = new CancellationTokenSource();
        await using var stdoutFile = File.Create(prefix + NodeCaptureProtocol.StdoutSuffix);
        await using var stderrFile = File.Create(prefix + NodeCaptureProtocol.StderrSuffix);
        Task stdout = process.StandardOutput.BaseStream.CopyToAsync(stdoutFile, captureStop.Token);
        Task stderr = process.StandardError.BaseStream.CopyToAsync(stderrFile, captureStop.Token);
        bool timedOut = false;
        bool rawComplete = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(NodeNames.TimeoutSeconds));
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
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(NodeNames.KilledProcessWaitSeconds));
        }
        finally
        {
            if (process.HasExited)
                await File.WriteAllTextAsync(
                    prefix + NodeCaptureProtocol.ExitSuffix,
                    process.ExitCode.ToString(CultureInfo.InvariantCulture) + NodeCaptureProtocol.NewLine
                );
            if (!process.HasExited)
                captureStop.Cancel();
            try
            {
                await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(NodeNames.StreamDrainSeconds));
                rawComplete = true;
            }
            catch (TimeoutException)
            {
                await File.WriteAllTextAsync(prefix + NodeCaptureProtocol.CompleteSuffix, NodeCaptureProtocol.CompleteFalse);
                captureStop.Cancel();
                process.StandardOutput.Close();
                process.StandardError.Close();
                try
                {
                    await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(NodeNames.CaptureCancellationJoinSeconds));
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
                prefix + NodeCaptureProtocol.CompleteSuffix,
                rawComplete ? NodeCaptureProtocol.CompleteTrue : NodeCaptureProtocol.CompleteFalse
            );
        }

        return new(
            process.ExitCode,
            timedOut,
            rawComplete
        );
    }
}
