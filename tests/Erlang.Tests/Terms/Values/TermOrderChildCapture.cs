using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class TermOrderChildCapture
{
    internal static async Task<TermOrderChildReceipt> Run(
        ProcessStartInfo start,
        string family,
        string operation,
        int depth,
        string expectedTermsHash,
        string driver,
        string directory
    )
    {
        Directory.CreateDirectory(directory);
        string outputPath = Path.Combine(directory, TermOrderHostNames.StandardOutputFile);
        string errorPath = Path.Combine(directory, TermOrderHostNames.StandardErrorFile);
        using var outputFile = new FileStream(
            outputPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.ReadWrite,
            1,
            useAsync: true
        );
        using var errorFile = new FileStream(
            errorPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.ReadWrite,
            1,
            useAsync: true
        );
        using var cancellation = new CancellationTokenSource();
        var clock = Stopwatch.StartNew();
        var failures = new List<string>();
        Process? process = null;
        Task? output = null, error = null;
        bool timedOut = false, killRequested = false, settled = false;
        int? pid = null, exitCode = null;
        TermOrderChildReceipt? receipt = null;
        try
        {
            process = Process.Start(start) ?? throw new InvalidOperationException(nameof(Process.Start));
            pid = process.Id;
            output = process.StandardOutput.BaseStream.CopyToAsync(outputFile, cancellation.Token);
            error = process.StandardError.BaseStream.CopyToAsync(errorFile, cancellation.Token);
            await process.WaitForExitAsync().WaitAsync(TermOrderHostNames.Deadline);
        }
        catch (TimeoutException failure)
        {
            timedOut = true;
            failures.Add(failure.ToString());
        }
        catch (Exception failure)
        {
            failures.Add(failure.ToString());
        }
        finally
        {
            if (process is not null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        killRequested = true;
                        try
                        {
                            process.Kill(entireProcessTree: true);
                        }
                        catch (InvalidOperationException) when (process.HasExited)
                        {
                        }
                    }
                }
                catch (Exception failure)
                {
                    failures.Add(failure.ToString());
                }
                try
                {
                    await process.WaitForExitAsync().WaitAsync(TermOrderHostNames.JoinDeadline);
                    settled = process.HasExited;
                    if (settled)
                        exitCode = process.ExitCode;
                }
                catch (Exception failure)
                {
                    failures.Add(failure.ToString());
                }
            }
            Task streams = Task.WhenAll(output ?? Task.CompletedTask, error ?? Task.CompletedTask);
            try
            {
                await streams.WaitAsync(TermOrderHostNames.JoinDeadline);
            }
            catch (Exception failure)
            {
                failures.Add(failure.ToString());
                cancellation.Cancel();
                try
                {
                    process?.StandardOutput.Dispose();
                    process?.StandardError.Dispose();
                    await streams.WaitAsync(TermOrderHostNames.JoinDeadline);
                }
                catch (Exception finalFailure)
                {
                    failures.Add(finalFailure.ToString());
                }
            }
            try
            {
                await Task.WhenAll(outputFile.FlushAsync(), errorFile.FlushAsync()).WaitAsync(TermOrderHostNames.JoinDeadline);
            }
            catch (Exception failure)
            {
                failures.Add(failure.ToString());
            }
            CloseFile(outputFile, failures);
            CloseFile(errorFile, failures);
            string outputHash = HashFile(outputPath, failures), errorHash = HashFile(errorPath, failures);
            receipt = new TermOrderChildReceipt(
                family,
                operation,
                depth,
                pid,
                clock.Elapsed.TotalSeconds,
                exitCode,
                process is not null,
                timedOut,
                killRequested,
                settled,
                output?.IsCompletedSuccessfully == true,
                error?.IsCompletedSuccessfully == true,
                failures.ToArray(),
                expectedTermsHash,
                driver,
                outputHash,
                errorHash
            );
            await File.WriteAllTextAsync(Path.Combine(directory, TermOrderHostNames.ReceiptFile), JsonSerializer.Serialize(receipt));
            process?.Dispose();
        }

        return receipt!;
    }

    private static void CloseFile(FileStream file, List<string> failures)
    {
        try
        {
            file.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure.ToString());
        }
    }

    private static string HashFile(string path, List<string> failures)
    {
        try
        {
            return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        }
        catch (Exception failure)
        {
            failures.Add(failure.ToString());

            return string.Empty;
        }
    }
}
