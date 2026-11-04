using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang.Differential;

if (args.Length != DescriptorPollFdValidationNames.ArgumentCount || !OperatingSystem.IsLinux()) return DescriptorPollFdValidationNames.UsageExitCode;
string mode = args[0], oracle = args[1], report = args[2];
bool passed = false;
Exception? failure = null;
string? details = null;
try
{
    if (mode == DescriptorPollFdValidationNames.Compare)
    {
        details = report + DescriptorPollFdValidationNames.DetailsSuffix;
        int code = await DescriptorIoContracts.RunReference(oracle, details);
        passed = code == 0;
    }
    else if (mode == DescriptorPollFdValidationNames.Cancel)
    {
        await DescriptorIoContracts.RunCancellation();
        passed = true;
    }
    else throw new ArgumentException(nameof(mode));
}
catch (Exception error)
{
    failure = error;
}
string json = JsonSerializer.Serialize(
    new
    {
        Passed = passed,
        Mode = mode,
        Runtime = Environment.Version.ToString(),
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        OriginalComparisonCount = mode == DescriptorPollFdValidationNames.Compare ? DescriptorIoCases.All.Count : 0,
        ExistingCancellationCase = mode == DescriptorPollFdValidationNames.Cancel ? DescriptorIoNames.CancellationTest : null,
        DetailsPath = details,
        ErrorType = failure?.GetType().FullName,
        Error = failure?.ToString(),
        Scope = DescriptorPollFdValidationNames.Scope
    },
    new JsonSerializerOptions { WriteIndented = true }
);
Console.WriteLine(json);
await File.WriteAllTextAsync(report, json);
return passed ? 0 : 1;
