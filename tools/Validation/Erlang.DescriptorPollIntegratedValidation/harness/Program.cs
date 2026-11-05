using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

var cases = new List<object>();
bool passed = true;
try
{
    if (args.Length != 2 || !OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        throw new PlatformNotSupportedException(PollHarnessNames1557.Contract);
    string[] selected = args[0] == PollHarnessNames1557.All ? PollHarnessNames1557.Cases : [args[0]];
    foreach (string name in selected)
    {
        try
        {
            object result = await PollHarnessCases1557.Run(name);
            cases.Add(new { Name = name, Passed = true, Result = result });
        }
        catch (Exception error)
        {
            passed = false;
            cases.Add(new
            {
                Name = name,
                Passed = false,
                ErrorType = error.GetType().FullName,
                Error = error.ToString(),
                CleanupSucceeded = error.Data[PollHarnessNames1557.CleanupSucceeded],
                PhysicalLifetimeEnded = error.Data[PollHarnessNames1557.PhysicalEnded],
                ActualThreadJoined = error.Data[PollHarnessNames1557.ThreadJoined],
                LostWakePendingBeforeCleanup = error.Data[PollHarnessNames1557.LostWakePending]
            });
        }
    }
}
catch (Exception error)
{
    passed = false;
    cases.Add(new { Name = "setup", Passed = false, Error = error.ToString() });
}
var report = new
{
    Passed = passed,
    Cases = cases,
    Host = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    Runtime = Environment.Version.ToString(),
    Scope = "Genuine Linux x64 component poll engine only; actual pipes/kernel thread/syscalls/MSACC. No port/VM integration, OTP equality, active external FD reuse or blocking data-transfer cancellation claim."
};
string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
if (args.Length == 2) File.WriteAllText(args[1], json);
Environment.ExitCode = passed ? 0 : 1;
