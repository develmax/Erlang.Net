using Erlang;
using Erlang.Differential;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length is < 3 or > 4 || args[0] is not (SupplementalCleanupNames.SdkMode or SupplementalCleanupNames.OriginalMode) || (args[0] == SupplementalCleanupNames.OriginalMode) != (args.Length == 4))
    throw new ArgumentException(SupplementalCleanupNames.Usage);
string mode = args[0];
string sourcePath = Path.GetFullPath(args[1]);
string reportPath = Path.GetFullPath(args[2]);
if (File.Exists(reportPath))
    throw new InvalidOperationException(SupplementalCleanupNames.ExistingReport);
byte[] sourceBytes = await File.ReadAllBytesAsync(sourcePath);
string source = new UTF8Encoding(false, true).GetString(sourceBytes);
Term expected = Term.Tuple(
    Term.A(SupplementalCleanupNames.Success),
    Term.Tuple(Term.A(SupplementalCleanupNames.Killed), Term.A(SupplementalCleanupNames.NoCleanup))
);
Term? actual = null;
string? generatedHash = null;
string output = string.Empty;
string error = string.Empty;
string? failure = null;
int? originalExitCode = null;
try
{
    if (mode == SupplementalCleanupNames.SdkMode)
    {
        using var generated = GeneratedModuleCompiler.Compile(source);
        generatedHash = generated.CSharpSha256;
        CompiledModuleStreamResult captured = await CompiledModuleExecution.RunCaptured(generated);
        actual = captured.Result;
        output = captured.StandardOutputHex;
        error = captured.StandardErrorHex;
    }
    else
    {
        var start = new ProcessStartInfo(Path.GetFullPath(args[3]))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[] { SupplementalCleanupNames.NoShell, SupplementalCleanupNames.NoInput, SupplementalCleanupNames.Evaluate, CompiledModuleProtocol.Command(source) })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(SupplementalCleanupNames.OriginalDeadline);
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(SupplementalCleanupNames.CleanupDeadline);
            throw;
        }
        output = await stdout;
        error = await stderr;
        originalExitCode = process.ExitCode;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(SupplementalCleanupNames.OriginalFailed);
        actual = OracleResultProtocol.Decode(output);
    }
}
catch (Exception exception)
{
    failure = exception.ToString();
}
var identities = AppDomain.CurrentDomain.GetAssemblies()
    .Where(assembly => !assembly.IsDynamic && assembly.GetName().Name is { } name && (name.StartsWith(SupplementalCleanupNames.SubjectAssemblyPrefix, StringComparison.Ordinal) || name == SupplementalCleanupNames.CollectorAssembly))
    .Select(assembly => new
    {
        Name = assembly.GetName().Name,
        Path = assembly.Location,
        Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
    }).ToArray();
byte[] currentSourceBytes = await File.ReadAllBytesAsync(sourcePath);
bool sourceUnchanged = sourceBytes.SequenceEqual(currentSourceBytes);
bool passed = failure is null && sourceUnchanged && expected.Equals(actual) && (mode != SupplementalCleanupNames.SdkMode || output.Length == 0 && error.Length == 0);
var report = new
{
    Status = passed ? SupplementalCleanupNames.Pass : SupplementalCleanupNames.Fail,
    Mode = mode,
    SourcePath = sourcePath,
    SourceSha256 = Convert.ToHexString(SHA256.HashData(sourceBytes)),
    SourceUnchanged = sourceUnchanged,
    Depth = SupplementalCleanupNames.Depth,
    NoCleanupObservationMilliseconds = SupplementalCleanupNames.QuietMilliseconds,
    ReadyHandshakeRequired = true,
    MonitoredExitReasonRequired = SupplementalCleanupNames.Killed,
    Expected = expected.ToString(),
    Actual = actual?.ToString(),
    GeneratedCSharpSha256 = generatedHash,
    OriginalExecuted = mode == SupplementalCleanupNames.OriginalMode && originalExitCode is not null,
    OriginalExitCode = originalExitCode,
    StandardOutput = output,
    StandardError = error,
    Exception = failure,
    Assemblies = identities,
    Scope = SupplementalCleanupNames.Scope
};
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await using (FileStream file = new(
    reportPath,
    FileMode.CreateNew,
    FileAccess.Write,
    FileShare.Read
))
    await JsonSerializer.SerializeAsync(file, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(new { report.Status, report.Mode, report.Actual, report.SourceSha256 }));
return passed ? 0 : 1;
