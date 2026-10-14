using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Erlang.HaltProbe;

public static class HaltSubprocessRunner
{
    public static async Task<HaltSubprocessResult> Run(
        HaltSubprocessContract contract,
        HaltProcessBackend backend,
        string executable,
        string? probeAssembly,
        string artifactRoot,
        CancellationToken cancellation = default
    )
    {
        string directory = Path.Combine(Path.GetFullPath(artifactRoot), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        foreach (HaltSourceUnit unit in contract.Sources)
        {
            if (Path.GetFileName(unit.Module) != unit.Module || unit.Module.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException(unit.Module);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(unit.Source)));
            if (hash != unit.SourceSha256)
                throw new InvalidDataException(unit.Module);
            string path = Path.Combine(directory, unit.Module + ".erl");
            await File.WriteAllTextAsync(
                path,
                unit.Source,
                new UTF8Encoding(false),
                cancellation
            );
            paths.Add(path);
        }
        string evidencePath = Path.Combine(directory, HaltProcessParameters.EvidenceFile);
        string stagePath = Path.Combine(directory, HaltProcessParameters.NativeStageFile);
        var start = new ProcessStartInfo(executable) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        if (backend == HaltProcessBackend.Original)
        {
            start.Environment[NativeHaltTrackNames.DumpEnvironment] = Path.Combine(directory, NativeHaltTrackNames.DumpFile);
            start.Environment.Remove(NativeHaltTrackNames.DumpSecondsEnvironment);
            start.Environment.Remove(NativeHaltTrackNames.DumpBytesEnvironment);
            start.Environment.Remove(NativeHaltTrackNames.DumpPublicKeyEnvironment);
            start.ArgumentList.Add("-noshell");
            start.ArgumentList.Add("-noinput");
            start.ArgumentList.Add("-eval");
            start.ArgumentList.Add(OriginalExpression(
                paths,
                stagePath,
                contract.Module,
                contract.Entry
            ));
        }
        else
        {
            ArgumentNullException.ThrowIfNull(probeAssembly);
            start.ArgumentList.Add(Path.GetFullPath(probeAssembly));
            foreach (string path in paths)
            {
                start.ArgumentList.Add(HaltProbeNames.Source);
                start.ArgumentList.Add(path);
            }
            foreach (string argument in new[] { HaltProbeNames.Module, contract.Module, HaltProbeNames.Entry, contract.Entry, HaltProbeNames.Backend, backend == HaltProcessBackend.Compiled ? HaltProbeNames.Compiled : HaltProbeNames.Interpreted, HaltProbeNames.Evidence, evidencePath, HaltProbeNames.Dump, Path.Combine(directory, HaltProcessParameters.DumpFile) })
                start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException(executable);
        Task<byte[]> output = Read(process.StandardOutput.BaseStream, Path.Combine(directory, HaltProcessParameters.RawStandardOutputFile));
        Task<byte[]> error = Read(process.StandardError.BaseStream, Path.Combine(directory, HaltProcessParameters.RawStandardErrorFile));
        bool timeout = false;
        string? failure = null;

        async Task SaveRaw()
        {
            int? exitCode = process.HasExited ? process.ExitCode : null;
            var raw = new HaltRawProcessEvidence(
                process.Id,
                exitCode,
                timeout,
                output.IsCompletedSuccessfully,
                error.IsCompletedSuccessfully,
                failure
            );
            await File.WriteAllTextAsync(
                Path.Combine(directory, HaltProcessParameters.RawProcessEvidenceFile),
                JsonSerializer.Serialize(raw, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        await SaveRaw();
        try
        {
            await process.WaitForExitAsync(cancellation).WaitAsync(TimeSpan.FromSeconds(HaltProcessParameters.ProcessTimeoutSeconds), cancellation);
        }
        catch (Exception exception) when (exception is TimeoutException or OperationCanceledException)
        {
            timeout = exception is TimeoutException;
            failure = exception.ToString();
            try
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch (InvalidOperationException) when (process.HasExited) { }
                }
            }
            catch (Exception killFailure) when (killFailure is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException)
            {
                failure += Environment.NewLine + killFailure;
                await SaveRaw();
                process.StandardOutput.Dispose();
                process.StandardError.Dispose();
                try
                {
                    await Task.WhenAll(output, error);
                }
                catch (Exception captureFailure) when (captureFailure is IOException or ObjectDisposedException)
                {
                    failure += Environment.NewLine + captureFailure;
                }
                await SaveRaw();
                throw new IOException(HaltProcessParameters.TerminationFailure, killFailure);
            }
            await process.WaitForExitAsync();
        }
        try
        {
            await Task.WhenAll(output, error);
        }
        catch (Exception captureFailure) when (captureFailure is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            failure ??= captureFailure.ToString();
            await SaveRaw();
            throw;
        }
        byte[] standardOutput = output.Result, standardError = error.Result;
        await SaveRaw();
        HaltProbeEvidence? evidence = null;
        string stage = timeout ? HaltProcessParameters.TimedOut : HaltProcessParameters.ProcessExited;
        if (backend == HaltProcessBackend.Original)
        {
            if (File.Exists(stagePath))
            {
                try
                {
                    stage = await File.ReadAllTextAsync(stagePath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    failure ??= exception.ToString();
                }
            }
        }
        else if (File.Exists(evidencePath))
        {
            try
            {
                evidence = JsonSerializer.Deserialize<HaltProbeEvidence>(await File.ReadAllTextAsync(evidencePath));
                stage = evidence?.Stage ?? HaltProcessParameters.NoEvidence;
                if (evidence?.Error is not null)
                    failure ??= evidence.Error;
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                failure ??= exception.ToString();
            }
        }
        else
            failure ??= HaltProcessParameters.NoEvidence;

        await SaveRaw();

        return new(
            contract.Name,
            backend,
            stage,
            process.ExitCode,
            Array.AsReadOnly(standardOutput),
            Array.AsReadOnly(standardError),
            timeout,
            evidence,
            failure,
            directory,
            contract.Sources
        );
    }

    private static async Task<byte[]> Read(Stream stream, string rawPath)
    {
        using var buffer = new MemoryStream();
        await using var raw = new FileStream(
            rawPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            HaltProcessParameters.StreamCaptureBufferBytes,
            FileOptions.Asynchronous
        );
        byte[] chunk = new byte[HaltProcessParameters.StreamCaptureBufferBytes];
        int count;
        while ((count = await stream.ReadAsync(chunk)) != 0)
        {
            await buffer.WriteAsync(chunk.AsMemory(0, count));
            await raw.WriteAsync(chunk.AsMemory(0, count));
            await raw.FlushAsync();
        }

        return buffer.ToArray();
    }

    private static string OriginalExpression(
        IReadOnlyList<string> sources,
        string stage,
        string module,
        string entry
    )
    {
        string stamp(string value) => "ok=file:write_file(" + Quote(stage) + "," + Quote(value) + "),";

        string atom(string value) => "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
        var expression = new StringBuilder(stamp(HaltProcessParameters.CompileStage));
        foreach (string path in sources)
            expression.Append("case compile:file(").Append(Quote(path)).Append(",[return_errors,return_warnings]) of {ok,_}->ok;{ok,_,_}->ok;E->erlang:error({compile_failed,E}) end,");
        expression.Append(stamp(HaltProcessParameters.ExecuteStage));
        expression.Append("apply(").Append(atom(module)).Append(',').Append(atom(entry)).Append(",[]),");
        expression.Append(stamp(HaltProcessParameters.ReturnStage));
        expression.Append("erlang:halt(0).");

        return expression.ToString();
    }

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
