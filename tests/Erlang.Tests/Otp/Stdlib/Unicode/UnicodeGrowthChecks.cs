using System.Text.Json;
using static Erlang.Tests.UnicodeBackendNames;

namespace Erlang.Tests;

internal static class UnicodeGrowthChecks
{
    internal static async Task Run()
    {
        var choices = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed("lists"),
            RuntimeModuleSelection.Beam("unicode_util", Read(StringBackendNames.Prefix, "unicode_util.beam"), ["lists"]),
            RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(choices, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        using PreparedBeamImage observer = runtime.Modules.PrepareBeam(
            Read(ResourcePrefix, GrowthObserver + ".beam"),
            new BeamDependencySelection([GrowthObserver, "unicode", "lists", "erlang", "binary", "file", "filename"])
        );
        await runtime.InitializeModulesAsync(registry => Require(registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published));
        Term[] rows = Cons.Items(ExternalTermFormat.Decode(Read(ResourcePrefix, "growth.etf"))).ToArray();
        var observations = new List<(int Count, string Encoding, long Allocated)>();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (TupleTerm row in rows)
        {
            await actor.InvokeAsync(async context =>
            {
                long before = GC.GetTotalAllocatedBytes(precise: true);
                Term actual = await runtime.Modules.Call(
                    context,
                    GrowthObserver,
                    "run",
                    row.Items[0],
                    row.Items[1]
                );
                long allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
                Require(actual.Equals(row.Items[2]));
                observations.Add(((int)((Integer)row.Items[0]).Value, row.Items[1].ToString()!, allocated));

                return true;
            });
            if ((int)((Integer)row.Items[0]).Value == AllocationLargeCount)
            {
                var large = observations[^1];
                long small = observations.Single(value => value.Count == AllocationSmallCount && value.Encoding == large.Encoding).Allocated;
                Require(large.Allocated <= small * MaximumAllocationDoublingFactor + AllocationNoiseBytes);
            }
        }
        string directory = Path.Combine("artifacts", "unicode-growth");
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "allocations.json"),
            JsonSerializer.Serialize(
                observations.Select(value => new { value.Count, value.Encoding, value.Allocated }),
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }

    private static byte[] Read(string prefix, string file)
    {
        using Stream input = typeof(UnicodeGrowthChecks).Assembly.GetManifestResourceStream(prefix + file)!;
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(GrowthFailure);
        }
    }
}
