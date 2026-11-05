using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static MultiSchedulingProbeNames;

if (args.Length != 1)
{
    Console.Error.WriteLine(Usage);

    return 2;
}
if (File.Exists(args[0]))
    throw new InvalidOperationException(ExistingReport);
var checks = new (string Name, Func<Task> Run)[]
{
    (NormalLanes, MultiSchedulingPhysicalChecks.Normal),
    (NormalDrain, MultiSchedulingPhysicalChecks.DrainNormal),
    (DirtyLazy, () => MultiSchedulingPhysicalChecks.QueueWhileBlocked(Lanes, false)),
    (DirtyNormal, () => MultiSchedulingPhysicalChecks.QueueWhileBlocked(Lanes, true)),
    (DirtyDrain, MultiSchedulingPhysicalChecks.ActiveDirty),
    (CancelPending, MultiSchedulingPhysicalChecks.CancelPendingBlocker),
    (CloseQueued, () => MultiSchedulingPhysicalChecks.Close(false)),
    (CloseActive, () => MultiSchedulingPhysicalChecks.Close(true)),
    (SingleLane, () => MultiSchedulingPhysicalChecks.QueueWhileBlocked(1, false)),
    (Repeated, MultiSchedulingPhysicalChecks.RepeatedBlocks),
    (NodeDistPhysicalNames.Legacy, () => NodeDistPhysicalChecks.StableRoots(false)),
    (NodeDistPhysicalNames.Physical, () => NodeDistPhysicalChecks.StableRoots(true)),
    (NodeDistPhysicalNames.Handles, NodeDistPhysicalChecks.PendingHandle)
};
var results = new List<object>();
int passed = 0;
foreach (var check in checks)
{
    try
    {
        await check.Run();
        passed++;
        results.Add(new { check.Name, Passed = true });
        Console.WriteLine(check.Name);
    }
    catch (Exception error)
    {
        results.Add(new { check.Name, Passed = false, Error = error.ToString() });
        Console.Error.WriteLine(error);
    }
}
string report = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(report)!);
File.WriteAllText(
    report,
    JsonSerializer.Serialize(
        new
        {
            Baseline,
            Passed = passed,
            Count = checks.Length,
            Results = results,
            RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
            RunnerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(MultiSchedulingPhysicalChecks).Assembly.Location)))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);

return passed == checks.Length ? 0 : 1;
