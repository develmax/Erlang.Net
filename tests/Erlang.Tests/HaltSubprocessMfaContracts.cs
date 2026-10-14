using System.Text.Json;
using Erlang.HaltProbe;

namespace Erlang.Tests;

public static class HaltSubprocessMfaContracts
{
    public static HaltSubprocessContract? Find(string module, string function, int arity)
        => module == ErlangModuleNames.Module && function == HaltSubprocessMfaNames.Function && arity >= HaltSubprocessMfaNames.FirstArity && arity <= HaltSubprocessMfaNames.LastArity
            ? HaltSubprocessCases.Direct(module, function, arity)
            : null;

    public static async Task Run(HaltSubprocessContract contract)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(HaltSubprocessMfaNames.ChildBudgetSeconds));
        string executable = Path.GetFullPath(
            Path.Combine(PortHostParameters.RuntimeRoot, OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
        );
        HaltSubprocessResult result = await HaltSubprocessRunner.Run(
            contract,
            HaltProcessBackend.Compiled,
            executable,
            HaltSubprocessHostNames.Probe,
            HaltSubprocessMfaNames.Artifacts,
            cancellation.Token
        );
        await File.WriteAllTextAsync(
            Path.Combine(result.WorkingDirectory, HaltSubprocessMfaNames.Report),
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        );
        IReadOnlyList<string> errors = HaltSubprocessVerifier.Check(contract, result);
        if (errors.Count != 0)
            throw new InvalidOperationException(HaltSubprocessMfaNames.Failure + HaltSubprocessHostNames.ContractSeparator + string.Join(HaltSubprocessHostNames.FailureListSeparator, errors));
    }
}
