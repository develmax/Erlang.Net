using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

var results = new List<object>();
bool passed = true;
try
{
    if (args.Length != 2 || !OperatingSystem.IsLinux()) throw new PlatformNotSupportedException(PollVmNames1570.Contract);
    string[] selected = args[0] == PollVmNames1570.All ? PollVmNames1570.Cases : [args[0]];
    foreach (string name in selected)
    {
        try
        {
            object result = await PollVmChecks1570.Run(name);
            results.Add(new { Name = name, Passed = true, Result = result });
        }
        catch (Exception error)
        {
            passed = false;
            results.Add(new { Name = name, Passed = false, Error = error.ToString() });
        }
    }
}
catch (Exception error)
{
    passed = false;
    results.Add(new { Name = "setup", Passed = false, Error = error.ToString() });
}
string json = JsonSerializer.Serialize(
    new
    {
        Passed = passed,
        Cases = results,
        Host = RuntimeInformation.OSDescription,
        Runtime = Environment.Version.ToString(),
        Scope = "Actual Linux logical VM bootstrap and poll ownership lifecycle observer; no OTP equality, fd data-transfer or full statistics acceptance claim."
    },
    new JsonSerializerOptions { WriteIndented = true }
);
Console.WriteLine(json);
if (args.Length == 2) File.WriteAllText(args[1], json);
Environment.ExitCode = passed ? 0 : 1;
