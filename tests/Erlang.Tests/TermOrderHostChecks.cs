using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

public static class TermOrderHostChecks
{
    public static Task DeepCompare() => Families(TermOrderHostNames.Compare);

    public static Task DeepHash() => Families(TermOrderHostNames.Hash);

    public static Task Shallow() => Child(TermOrderHostNames.Tuple, TermOrderHostNames.Shallow, 6);

    public static Task Snapshot() => Child(TermOrderHostNames.Tuple, TermOrderHostNames.Snapshot, 0);

    private static async Task Families(string operation)
    {
        foreach (string family in new[]
        {
            TermOrderHostNames.Tuple,
            TermOrderHostNames.ListHead,
            TermOrderHostNames.MapValue,
            TermOrderHostNames.FunctionEnvironment,
            TermOrderHostNames.NativeRecord
        })
            await Child(family, operation, TermOrderHostNames.MaximumDepth);
    }

    private static async Task Child(string family, string operation, int depth)
    {
        string host = Environment.ProcessPath ?? throw new InvalidOperationException(nameof(Environment.ProcessPath));
        string driver = typeof(TermOrderHostChecks).Assembly.Location;
        string expectedTermsPath = typeof(Term).Assembly.Location;
        string expectedTermsHash = HashFile(expectedTermsPath);
        bool dotnet = Path.GetFileNameWithoutExtension(host).Equals(TermOrderHostNames.DotnetHost, StringComparison.OrdinalIgnoreCase);
        string executable = dotnet ? host : Path.Combine(
            Path.GetDirectoryName(driver)!,
            Path.GetFileNameWithoutExtension(driver) +
            (OperatingSystem.IsWindows() ? TermOrderHostNames.WindowsExecutableExtension : string.Empty)
        );
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        if (dotnet)
            start.ArgumentList.Add(driver);
        start.ArgumentList.Add(TermOrderHostNames.ChildSwitch);
        start.ArgumentList.Add(family);
        start.ArgumentList.Add(operation);
        start.ArgumentList.Add(depth.ToString(CultureInfo.InvariantCulture));
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            TermOrderHostNames.ArtifactsRoot,
            TermOrderHostNames.ArtifactDirectory,
            Guid.NewGuid().ToString(TermOrderHostNames.GuidFormat)
        );
        var receipt = await TermOrderChildCapture.Run(
            start,
            family,
            operation,
            depth,
            expectedTermsHash,
            driver,
            directory
        );
        if (!receipt.Started || !receipt.Settled || receipt.TimedOut || receipt.ExitCode != 0 ||
            !receipt.StandardOutputComplete || !receipt.StandardErrorComplete || receipt.LifecycleFailures.Length != 0)
            throw new InvalidOperationException(TermOrderHostNames.BrokenChild + directory);
        string output = await File.ReadAllTextAsync(Path.Combine(directory, TermOrderHostNames.StandardOutputFile));
        string error = await File.ReadAllTextAsync(Path.Combine(directory, TermOrderHostNames.StandardErrorFile));
        if (error.Length != 0)
            throw new InvalidOperationException(TermOrderHostNames.BrokenChild + directory);
        string[] lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        using var identity = JsonDocument.Parse(lines[0]);
        if (identity.RootElement.GetProperty(TermOrderHostNames.LoadedAssemblyHash).GetString() != expectedTermsHash ||
            !string.Equals(
                Path.GetFullPath(identity.RootElement.GetProperty(TermOrderHostNames.LoadedAssemblyPath).GetString()!),
                Path.GetFullPath(expectedTermsPath),
                StringComparison.OrdinalIgnoreCase
            ) ||
            lines[^1] != TermOrderHostNames.Passed ||
            (operation is TermOrderHostNames.Compare or TermOrderHostNames.Hash && !lines.Contains(TermOrderHostNames.Constructed)))
            throw new InvalidOperationException(TermOrderHostNames.BrokenChild + directory);
    }

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
