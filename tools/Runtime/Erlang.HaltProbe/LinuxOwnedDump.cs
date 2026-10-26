using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Erlang.HaltProbe;

internal static class LinuxOwnedDump
{
    public static void Write(string dump)
    {
        string executable = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), HaltProbeNames.CreateDump);
        if (!File.Exists(executable))
            throw new FileNotFoundException(HaltProbeNames.CreateDump, executable);
        var start = new ProcessStartInfo(HaltProbeNames.LinuxShell) { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in new[] { "-c", HaltProbeNames.DumpGateScript, HaltProbeNames.DumpGate, executable, HaltProbeNames.NormalDumpOption, HaltProbeNames.DumpNameOption, dump.Replace("%", "%%"), Environment.ProcessId.ToString(CultureInfo.InvariantCulture) })
            start.ArgumentList.Add(argument);
        using var child = Process.Start(start) ?? throw new InvalidOperationException(executable);
        Task output = child.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardError());
        Task error = child.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError());
        bool allowed = false;
        try
        {
            if (Prctl(
                HaltProbeNames.LinuxSetPtracer,
                (nuint)child.Id,
                0,
                0,
                0
            ) != 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            allowed = true;
            child.StandardInput.WriteLine(HaltProbeNames.DumpGate);
            child.StandardInput.Close();
            child.WaitForExit();
            Task.WhenAll(output, error).GetAwaiter().GetResult();
            if (child.ExitCode != 0 || !File.Exists(dump))
                throw new InvalidOperationException(HaltProbeNames.CreateDump + ":" + child.ExitCode);
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill(true);
                child.WaitForExit();
            }
            Task.WhenAll(output, error).GetAwaiter().GetResult();
            if (allowed && Prctl(
                HaltProbeNames.LinuxSetPtracer,
                0,
                0,
                0,
                0
            ) != 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }

    [DllImport(HaltProbeNames.UnixRuntime, EntryPoint = "prctl", SetLastError = true)]
    private static extern int Prctl(
        int option,
        nuint argument2,
        nuint argument3,
        nuint argument4,
        nuint argument5
    );
}
