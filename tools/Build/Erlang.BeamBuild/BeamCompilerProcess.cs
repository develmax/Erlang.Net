using System.Diagnostics;
using System.Text;

namespace Erlang.BeamBuild;

internal static class BeamCompilerProcess
{
    internal static Task Run(
        AdmittedCompiler compiler,
        string source,
        string output,
        string logPrefix
    )
        => RunCore(
            compiler,
            source,
            output,
            logPrefix,
            output,
            [BeamCompilerArgumentNames.OutputDirectory, output, source]
        );

    internal static Task RunEpp(
        AdmittedCompiler runtime,
        string driverDirectory,
        string requestFile,
        string ownedInputs,
        string output,
        string logPrefix
    )
        => RunCore(
            runtime,
            requestFile,
            output,
            logPrefix,
            ownedInputs,
            [BeamEppDriverNames.NoShell, BeamEppDriverNames.NoInput,
             BeamEppDriverNames.Boot, BeamEppDriverNames.NoDotErlangBoot,
             BeamEppDriverNames.PrependPath, driverDirectory,
             BeamEppDriverNames.StartModule, BeamEppDriverNames.Module, BeamEppDriverNames.Entry,
             BeamEppDriverNames.Extra, requestFile]
        );

    internal static Task RunApplication(
        AdmittedCompiler runtime,
        string driverDirectory,
        string requestFile,
        string ownedInputs,
        string output,
        string logPrefix
    )
        => RunCore(
            runtime,
            requestFile,
            output,
            logPrefix,
            ownedInputs,
            [BeamEppDriverNames.NoShell, BeamEppDriverNames.NoInput,
             BeamEppDriverNames.Boot, BeamEppDriverNames.NoDotErlangBoot,
             BeamEppDriverNames.PrependPath, driverDirectory,
             BeamEppDriverNames.StartModule, BeamApplicationDriverNames.Module, BeamApplicationDriverNames.Entry,
             BeamEppDriverNames.Extra, requestFile]
        );

    private static async Task RunCore(
        AdmittedCompiler compiler,
        string source,
        string output,
        string logPrefix,
        string workingDirectory,
        IReadOnlyList<string> arguments
    )
    {
        CompilerExecutionClosure.Validate(compiler);
        if (BeamBuildStorage.HashFile(compiler.Executable) != compiler.Sha256)
            throw new InvalidDataException(BeamBuildNames.Admission);
        var start = new ProcessStartInfo(compiler.Executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };
        foreach (string name in BeamBuildNames.RemovedEnvironment)
            start.Environment.Remove(name);
        start.Environment[BeamBuildNames.EmulatorFlags] = BeamBuildNames.EmulatorFlagsValue;
        start.Environment[BeamBuildNames.CompilerServer] = BeamBuildNames.CompilerServerDisabled;
        foreach (string argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException(BeamBuildNames.Admission);
        Task<string> stdout = Drain(process.StandardOutput);
        Task<string> stderr = Drain(process.StandardError);
        using var deadline = new CancellationTokenSource(
            TimeSpan.FromSeconds(BeamCompilerProcessLimits.TimeoutSeconds),
            BeamCompilerDeadlineClock.Current
        );
        var failures = new List<Exception>();
        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (Exception error)
        {
            timedOut = error is OperationCanceledException && deadline.IsCancellationRequested;
            failures.Add(error);
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(BeamCompilerProcessLimits.CleanupSeconds));
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
        }
        await SaveLog(logPrefix + BeamBuildArtifactNames.StdoutLogSuffix, stdout, failures);
        await SaveLog(logPrefix + BeamBuildArtifactNames.StderrLogSuffix, stderr, failures);
        try
        {
            BeamBuildStorage.AtomicJson(
                logPrefix + BeamBuildArtifactNames.ProcessReceiptSuffix,
                new
                {
                    Executable = compiler.Executable,
                    CompilerSha256 = compiler.Sha256,
                    Source = source,
                    Output = output,
                    WorkingDirectory = workingDirectory,
                    Arguments = arguments,
                    TimedOut = timedOut,
                    ExitCode = process.HasExited ? process.ExitCode : (int?)null,
                    Shell = false,
                    RemovedEnvironment = BeamBuildNames.RemovedEnvironment,
                    EmulatorFlags = BeamBuildNames.EmulatorFlagsValue,
                    CompilerServer = BeamBuildNames.CompilerServerDisabled,
                    LogCharacterLimit = BeamCompilerProcessLimits.LogCharacters,
                    CleanupErrors = failures.Select(error => error.ToString()).ToArray(),
                    IndependentDescendantQuiescenceVerified = false
                }
            );
        }
        catch (Exception receiptError)
        {
            failures.Add(receiptError);
        }
        if (failures.Count != 0)
            throw new AggregateException(BeamBuildNames.Compilation, failures);
        CompilerExecutionClosure.Validate(compiler);
        if (process.ExitCode != 0 || BeamBuildStorage.HashFile(compiler.Executable) != compiler.Sha256)
            throw new InvalidDataException(BeamBuildNames.Compilation);
    }

    private static async Task SaveLog(string path, Task<string> text, List<Exception> failures)
    {
        try
        {
            await File.WriteAllTextAsync(path, await text.WaitAsync(TimeSpan.FromSeconds(BeamCompilerProcessLimits.CleanupSeconds)));
        }
        catch (Exception error)
        {
            failures.Add(error);
            _ = text.ContinueWith(
                task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }
    }

    private static async Task<string> Drain(StreamReader reader)
    {
        var saved = new StringBuilder();
        char[] buffer = new char[BeamCompilerProcessLimits.ReadBufferCharacters];
        int count;
        while ((count = await reader.ReadAsync(buffer)) != 0)
        {
            int keep = Math.Min(count, BeamCompilerProcessLimits.LogCharacters - saved.Length);
            if (keep > 0)
                saved.Append(buffer, 0, keep);
        }

        return saved.ToString();
    }
}
