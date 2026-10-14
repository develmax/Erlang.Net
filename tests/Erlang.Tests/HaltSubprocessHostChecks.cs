using System.Text.Json;
using Erlang.HaltProbe;

namespace Erlang.Tests;

internal static class HaltSubprocessHostChecks
{
    public static async Task Run()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(HaltSubprocessHostNames.OwnedProcessBudgetSeconds));
        await IoCharacterHostChecks.Run();
        string executable = Path.GetFullPath(
            Path.Combine(PortHostParameters.RuntimeRoot, OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
        );
        var results = new List<HaltSubprocessResult>();
        foreach (HaltProcessBackend backend in new[] { HaltProcessBackend.Interpreted, HaltProcessBackend.Compiled })
        {
            foreach (HaltSubprocessContract contract in HaltSubprocessCases.NumericAndErrors.Concat(HaltSubprocessCases.Adapters).Append(HaltSubprocessCases.Abort).Append(HaltSubprocessCases.CrashDump))
            {
                HaltSubprocessResult result = await HaltSubprocessRunner.Run(
                    contract,
                    backend,
                    executable,
                    HaltSubprocessHostNames.Probe,
                    HaltSubprocessHostNames.Artifacts,
                    cancellation.Token
                );
                results.Add(result);
                await File.WriteAllTextAsync(HaltSubprocessHostNames.Report, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                IReadOnlyList<string> errors = HaltSubprocessVerifier.Check(contract, result);
                if (errors.Count != 0)
                    throw new InvalidOperationException(HaltSubprocessHostNames.Failure + ":" + contract.Name + "/" + backend + ":" + string.Join(",", errors));
                if (contract.ExpectedRequestKind == RuntimeHaltKind.CrashDump)
                {
                    string path = Path.Combine(result.WorkingDirectory, HaltSubprocessHostNames.Dump);
                    using var stream = File.OpenRead(path);
                    byte[] header = new byte[HaltSubprocessHostNames.HeaderBytes];
                    await stream.ReadExactlyAsync(header);
                    byte[] expected = OperatingSystem.IsWindows() ? HaltSubprocessHostNames.WindowsDumpHeader : HaltSubprocessHostNames.LinuxDumpHeader;
                    if (!header.SequenceEqual(expected) || stream.Length <= header.Length || result.Evidence?.LogicalStack is null)
                        throw new InvalidOperationException(HaltSubprocessHostNames.Failure + ":" + path);
                }
            }
        }
        foreach (int arity in new[] { 0, 1, 2 })
        {
            HaltSubprocessContract direct = HaltSubprocessCases.Direct(ErlangModuleNames.Module, "halt", arity);
            if (!results.Any(result => result.Name == direct.Name && result.Backend == HaltProcessBackend.Compiled))
                throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
        }
    }
}
