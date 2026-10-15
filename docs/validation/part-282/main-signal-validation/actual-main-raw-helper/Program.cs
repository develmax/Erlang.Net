using DistributionPacketContrasts;

try
{
    string python = args[0];
    string prefix = Path.Combine(args[1], SignalRawNames.Prefix);
    Directory.CreateDirectory(args[1]);
    var result = await PacketProcess.Run(python, [SignalRawNames.PythonCode, SignalRawNames.ByteScript], prefix);
    if (result.ExitCode != SignalRawNames.ExitCode || result.TimedOut || !result.RawComplete ||
        !(await File.ReadAllBytesAsync(prefix + PacketCaptureProtocol.StdoutSuffix)).SequenceEqual(SignalRawNames.Stdout) ||
        !(await File.ReadAllBytesAsync(prefix + PacketCaptureProtocol.StderrSuffix)).SequenceEqual(SignalRawNames.Stderr))
        throw new InvalidOperationException(SignalRawNames.Failure);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { result.ExitCode, result.RawComplete, NativeExecuted = false }));

    string pipePrefix = Path.Combine(args[1], SignalRawNames.PipePrefix);
    string pidPath = Path.Combine(args[1], SignalRawNames.ChildPid);
    System.Diagnostics.Process? ownedChild = null;
    PacketProcessResult? pipeObservation = null;
    long pipeMilliseconds = 0;
    bool ownedChildTerminated = false;
    var watch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        var pipe = await PacketProcess.Run(python,
            [SignalRawNames.PythonCode, SignalRawNames.PipeScript, pidPath], pipePrefix);
        ownedChild = System.Diagnostics.Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(pidPath)));
        if (pipe.ExitCode != SignalRawNames.ExitCode || pipe.TimedOut || pipe.RawComplete || ownedChild.HasExited ||
            await File.ReadAllTextAsync(pipePrefix + PacketCaptureProtocol.CompleteSuffix) != PacketCaptureProtocol.CompleteFalse ||
            watch.Elapsed < TimeSpan.FromSeconds(PacketNames.StreamDrainSeconds))
            throw new InvalidOperationException(SignalRawNames.Failure);
        pipeObservation = pipe;
        pipeMilliseconds = watch.ElapsedMilliseconds;
    }
    finally
    {
        if (ownedChild is null && File.Exists(pidPath))
            ownedChild = System.Diagnostics.Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(pidPath)));
        if (ownedChild is not null)
        {
            using (ownedChild)
            {
                if (!ownedChild.HasExited)
                    ownedChild.Kill(entireProcessTree: true);
                await ownedChild.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(PacketNames.KilledProcessWaitSeconds));
                ownedChildTerminated = ownedChild.HasExited;
            }
        }
    }

    if (!ownedChildTerminated || pipeObservation is null)
        throw new InvalidOperationException(SignalRawNames.Failure);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
    {
        pipeObservation.ExitCode,
        pipeObservation.RawComplete,
        Milliseconds = pipeMilliseconds,
        OwnedChildTerminatedByHarness = ownedChildTerminated,
        NativeExecuted = false
    }));

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
