using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class PrimFileDeleteNativeOracle
{
    private static async Task<string> Execute(string file, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(file) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException(PrimFileDeleteNativeOracleNames.StartFailed);
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(1));
        if (process.ExitCode != 0) throw new InvalidOperationException(PrimFileDeleteNativeOracleNames.Failed + process.ExitCode + ": " + await stderr);

        return await stdout;
    }

    internal static async Task<HashSet<Term>> Capture(string repository, string output)
    {
        string source = Path.Combine(repository, PrimFileDeleteContractNames.Capture, PrimFileDeleteNativeOracleNames.Source);
        string executable = Path.Combine(output, PrimFileDeleteNativeOracleNames.Executable);
        _ = await Execute(
            PrimFileDeleteNativeOracleNames.Compiler,
            ["-std=c11", "-O2", "-pthread", "-Wall", "-Wextra", "-Werror", source, "-o", executable]
        );
        string raw = await Execute(executable, [output]);
        using JsonDocument result = JsonDocument.Parse(raw);
        int one = result.RootElement.GetProperty(PrimFileDeleteNativeOracleNames.OneSuccess).GetInt32();
        int both = result.RootElement.GetProperty(PrimFileDeleteNativeOracleNames.BothSuccess).GetInt32();
        if (one + both != PrimFileDeleteNativeOracleNames.Rounds || result.RootElement.GetProperty(PrimFileDeleteNativeOracleNames.RoundCount).GetInt32() != PrimFileDeleteNativeOracleNames.Rounds)
            throw new InvalidDataException(PrimFileDeleteNativeOracleNames.InvalidCapture);
        if (!result.RootElement.GetProperty(PrimFileDeleteNativeOracleNames.SequentialVerified).GetBoolean())
            throw new InvalidDataException(PrimFileDeleteNativeOracleNames.InvalidCapture);
        var admitted = new HashSet<Term>();
        admitted.Add(Term.List(
            Term.A(PrimFileDeleteNativeOracleNames.Ok),
            Term.Tuple(Term.A(PrimFileDeleteNativeOracleNames.Error), Term.A(PrimFileDeleteNativeOracleNames.Missing))
        ));
        if (both > 0) admitted.Add(Term.List(Term.A(PrimFileDeleteNativeOracleNames.Ok), Term.A(PrimFileDeleteNativeOracleNames.Ok)));
        File.WriteAllText(
            Path.Combine(output, PrimFileDeleteNativeOracleNames.Receipt),
            JsonSerializer.Serialize(
                new
                {
                    OriginalLeaf = PrimFileDeleteNativeOracleNames.OriginalLeaf,
                    SourceSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))),
                    ExecutableSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(executable))),
                    Observed = result.RootElement.Clone(),
                    Host = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    WholeOtpExecutionClaimed = false
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        return admitted;
    }
}
