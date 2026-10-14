using System.Diagnostics;
using System.Globalization;

namespace ApplyOptimizerContrasts;

internal static class ApplyProcess
{
    internal static async Task<ApplyProcessResult> Run(string executable, string[] arguments, string prefix)
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
        using var process = Process.Start(start) ?? throw new IOException(ApplyDiagnostics.ProcessStart);
        using var captureStop = new CancellationTokenSource();
        await using var stdoutFile = File.Create(prefix + ApplyCaptureProtocol.StdoutSuffix);
        await using var stderrFile = File.Create(prefix + ApplyCaptureProtocol.StderrSuffix);
        Task stdout = process.StandardOutput.BaseStream.CopyToAsync(stdoutFile, captureStop.Token);
        Task stderr = process.StandardError.BaseStream.CopyToAsync(stderrFile, captureStop.Token);
        bool timedOut = false;
        bool rawComplete = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(ApplyNames.TimeoutSeconds));
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
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(ApplyNames.KilledProcessWaitSeconds));
        }
        finally
        {
            if (process.HasExited)
                await File.WriteAllTextAsync(
                    prefix + ApplyCaptureProtocol.ExitSuffix,
                    process.ExitCode.ToString(CultureInfo.InvariantCulture) + ApplyCaptureProtocol.NewLine
                );
            if (!process.HasExited)
                captureStop.Cancel();
            try
            {
                await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(ApplyNames.StreamDrainSeconds));
                rawComplete = true;
            }
            catch (TimeoutException)
            {
                await File.WriteAllTextAsync(prefix + ApplyCaptureProtocol.CompleteSuffix, ApplyCaptureProtocol.CompleteFalse);
                captureStop.Cancel();
                process.StandardOutput.Close();
                process.StandardError.Close();
                try
                {
                    await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(ApplyNames.CaptureCancellationJoinSeconds));
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
                prefix + ApplyCaptureProtocol.CompleteSuffix,
                rawComplete ? ApplyCaptureProtocol.CompleteTrue : ApplyCaptureProtocol.CompleteFalse
            );
        }

        return new(
            process.ExitCode,
            timedOut,
            rawComplete
        );
    }
}
