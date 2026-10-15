using System.Diagnostics;

namespace BeamFixtureExport;

internal static class OracleCommand
{
    public static async Task<string> Run(string executable, string[] arguments, string logPrefix)
    {
        var start = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start) ?? throw new IOException(FixtureExportDiagnostics.ProcessStart);
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(OraclePin.ProcessTimeoutSeconds));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new TimeoutException(FixtureExportDiagnostics.ProcessTimeout(executable));
        }
        finally
        {
            await File.WriteAllTextAsync(logPrefix + OracleCommandNames.StandardOutputSuffix, await stdout);
            await File.WriteAllTextAsync(logPrefix + OracleCommandNames.StandardErrorSuffix, await stderr);
            await File.WriteAllTextAsync(
                logPrefix + OracleCommandNames.ExitCodeSuffix,
                process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n"
            );
        }
        if (process.ExitCode != 0)
        {
            throw new InvalidDataException(FixtureExportDiagnostics.ProcessExit(process.ExitCode, executable, logPrefix));
        }

        return await stdout;
    }
}
