using System.Diagnostics;
using System.Text.Json;

internal sealed class RestartPeer449 : IDisposable
{
    private readonly Process process;
    private readonly Task stdout;
    private readonly Task stderr;
    private readonly string directory;
    internal int Port { get; }
    internal int ProcessId => process.Id;

    internal RestartPeer449(string groupDirectory)
    {
        directory = Path.Combine(groupDirectory, "restarted-peer");
        Directory.CreateDirectory(directory);
        using var original = JsonDocument.Parse(File.ReadAllText(Path.Combine(groupDirectory, "peer.live.json")));
        string[] command = original.RootElement.GetProperty("Command").EnumerateArray().Select(e => e.GetString()!).ToArray();
        int oldId = original.RootElement.GetProperty("Pid").GetInt32();
        using var old = Process.GetProcessById(oldId);
        if (!old.WaitForExit(10000) || old.ExitCode != 0)
            throw new IOException("Original owned peer did not exit normally before restart");

        string ready = Path.Combine(directory, "ready");
        command[^1] = "ddll_tcp449:start([\"" + ready.Replace('\\', '/') + "\"]).";
        var start = new ProcessStartInfo(command[0])
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in command.Skip(1))
            start.ArgumentList.Add(argument);

        process = Process.Start(start) ?? throw new IOException("Owned restarted peer failed to launch");
        stdout = Capture(process.StandardOutput, Path.Combine(directory, "stdout.log"));
        stderr = Capture(process.StandardError, Path.Combine(directory, "stderr.log"));
        File.WriteAllText(Path.Combine(directory, "live.json"), JsonSerializer.Serialize(new { Pid = process.Id, PreviousPid = oldId, Command = command }));
        var deadline = Stopwatch.StartNew();
        while (!File.Exists(ready) && !process.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(20))
            Thread.Sleep(20);

        if (!File.Exists(ready) || process.HasExited || process.Id == oldId)
            throw new IOException("Owned restarted peer preparation failed");

        Port = int.Parse(File.ReadAllText(ready));
    }

    private static async Task Capture(StreamReader source, string destination)
    {
        await using var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write);
        await source.BaseStream.CopyToAsync(file);
    }

    public void Dispose()
    {
        bool forced = false;
        if (!process.WaitForExit(10000))
        {
            forced = true;
            process.Kill(entireProcessTree: true);
            process.WaitForExit(10000);
        }

        Task.WhenAll(stdout, stderr).Wait(TimeSpan.FromSeconds(10));
        File.WriteAllText(Path.Combine(directory, "exit.json"), JsonSerializer.Serialize(new { Pid = process.Id, ExitCode = process.ExitCode, ForcedTermination = forced, Settled = process.HasExited }));
        if (forced || process.ExitCode != 0)
            throw new IOException("Restarted peer did not exit normally");

        process.Dispose();
    }
}
