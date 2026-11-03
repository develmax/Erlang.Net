namespace Erlang.Tests;

internal static class ManagedContractBatch513ChildMain
{
    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != ManagedContractBatch513Names.ChildArgumentCount || arguments[0] != ManagedContractBatch513Names.ChildSwitch || !bool.TryParse(arguments[8], out bool owned))
            return 2;
        try
        {
            // The outer owned-process coordinator creates this working directory.
            // Each child owns exactly one VM/profile, including process globals.
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.GetFullPath(arguments[6]))!);
            using var diagnostics = Environment.GetEnvironmentVariable(ManagedContractBatch513Names.DiagnosticEnvironment)
                == ManagedContractBatch513Names.DiagnosticEnabled
                ? new ManagedUnsupportedDiagnostics520(Path.GetDirectoryName(Path.GetFullPath(arguments[6]))!) : null;
            await ManagedContractBatch513.CaptureAsync(
                arguments[1],
                arguments[2],
                arguments[3],
                arguments[4],
                arguments[5],
                arguments[6],
                arguments[7],
                owned
            );
            using var receipt = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(Path.Combine(arguments[6], ManagedContractBatch513Names.BatchFile)));

            return receipt.RootElement.GetProperty(ManagedContractBatch513Names.RunsProperty).EnumerateArray().All(run => run.GetProperty(ManagedContractBatch513Names.CapturedProperty).GetBoolean()) ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);

            return 1;
        }
    }
}
