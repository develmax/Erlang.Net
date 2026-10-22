using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Erlang.HaltProbe;

internal static class HaltWindowsDumpParent437
{
    internal static void Write(string dump)
    {
        using var current = Process.GetCurrentProcess();
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(),
            HaltWindowsDumpNames437.RuntimeRoot, HaltWindowsDumpNames437.Host));
        var start = new ProcessStartInfo(host)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // The helper is the identical project/assembly on the actual parent's CLR,
        // not another runtime implementation or a system-installed dump utility.
        foreach (string argument in new[]
        {
            HaltWindowsDumpNames437.FxVersion,
            Environment.Version.ToString(),
            typeof(HaltWindowsDumpParent437).Assembly.Location,
            HaltWindowsDumpNames437.Command,
            current.Id.ToString(CultureInfo.InvariantCulture),
            current.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture),
            Path.GetFullPath(dump)
        }) start.ArgumentList.Add(argument);
        using var child = Process.Start(start) ?? throw new InvalidOperationException(HaltWindowsDumpNames437.Failure);
        Task output = child.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardError());
        Task error = child.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError());
        try
        {
            if (!child.WaitForExit(checked(HaltProcessParameters.ProcessTimeoutSeconds * HaltWindowsDumpNames437.MillisecondsPerSecond)))
                throw new TimeoutException(HaltWindowsDumpNames437.Failure);
            Task.WhenAll(output, error).GetAwaiter().GetResult();
            if (child.ExitCode != 0)
                throw new InvalidOperationException(HaltWindowsDumpNames437.Failure + ":" + child.ExitCode);
            // Inspect the actual DbgHelp-produced MiscInfoStream independently in
            // the parent as well. A signature-only substitute cannot pass.
            HaltWindowsDumpIdentity437.Verify(dump, checked((uint)current.Id));
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill(true);
                child.WaitForExit();
            }
            Task.WhenAll(output, error).GetAwaiter().GetResult();
        }
    }
}
