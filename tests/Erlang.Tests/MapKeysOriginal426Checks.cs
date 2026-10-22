using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class MapKeysOriginal426Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(MapKeysOriginal426Names.Output,
            System.Guid.NewGuid().ToString(MapKeysOriginal426Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(1);
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapMapKeys426.Register(owner, registry);
                var declared = new BeamDependencySelection(new[] { MapKeysOriginal426Names.Runtime, MapKeysOriginal426Names.File, MapKeysOriginal426Names.Internal, MapKeysOriginal426Names.Debug, MapKeysOriginal426Names.Module }.Concat(MapKeysOriginal426Names.Helpers));
                var helpers = MapKeysOriginal426Names.Helpers.Select(module => registry.PrepareBeam(AtomicsHost413Access.Resource(module + ".beam"), declared)).ToArray();
                PreparedBeamImage same = registry.PrepareBeam(DebugSame420HostAccess.Beam(), declared);
                PreparedBeamImage worker = registry.PrepareBeam(MapKeysOriginal426Access.Resource(MapKeysOriginal426Names.WorkerBeam), declared);
                MapKeysOriginal426Access.Require(registry.PublishPreparedBeam(helpers.Append(same).Append(worker).ToArray()).Status == BeamPublicationStatus.Published);
                foreach (var image in helpers.Append(same).Append(worker))
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        MapKeysOriginal426Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        MapKeysOriginal426Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        MapKeysOriginal426Names.Runtime,
                        MapKeysOriginal426Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, MapKeysOriginal426Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, MapKeysOriginal426Names.Actual + file + MapKeysOriginal426Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(MapKeysOriginal426Names.Run, MapKeysOriginal426Names.Values);
            await Capture(MapKeysOriginal426Names.Stacks, MapKeysOriginal426Names.StackValues);
            await Capture(MapKeysOriginal426Names.Own, MapKeysOriginal426Names.OwnValues);
        }
        catch (Exception error)
        {
            errors.Add(new { Preparation = error.ToString() });
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { ActorCleanup = error.ToString() });
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { RuntimeCleanup = error.ToString() });
                }
        }
        // Decode expectations only after both independent routes and real owner cleanup.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        foreach (string file in new[] { MapKeysOriginal426Names.Values, MapKeysOriginal426Names.StackValues })
        {
            byte[] bytes = MapKeysOriginal426Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, MapKeysOriginal426Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, MapKeysOriginal426Names.Expected + file + MapKeysOriginal426Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == MapKeysOriginal426Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    MapKeysOriginal426Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    MapKeysOriginal426Names.Rows,
                    differences,
                    ref examined
                );
        }
        bool ownState = false;
        try
        {
            byte[] expectedOwn = MapKeysOriginal426Access.Resource(MapKeysOriginal426Names.OwnValues);
            File.WriteAllBytes(Path.Combine(output, MapKeysOriginal426Names.Expected + MapKeysOriginal426Names.OwnValues), expectedOwn);
            Term sourceOwn = OwnedFunctionOriginal407Terms.Decode(expectedOwn);
            File.WriteAllText(
                Path.Combine(output, MapKeysOriginal426Names.Expected + MapKeysOriginal426Names.OwnValues + MapKeysOriginal426Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(sourceOwn))
            );
            MapKeysOriginal426OwnState.Check(sourceOwn);
            MapKeysOriginal426OwnState.Check(captured[MapKeysOriginal426Names.OwnValues].Term);
            ownState = true;
        }
        catch (Exception error)
        {
            errors.Add(new { OwnStateProtocol = error.ToString() });
        }
        File.WriteAllText(
            Path.Combine(output, MapKeysOriginal426Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = MapKeysOriginal426Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                ActualOwnedFlatKeyTuple = true,
                OwnStateProtocol = ownState,
                OwnStateRows = 3,
                RawOwnStateRetained = true,
                OwnStateRawCrossVmEquality = false,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        MapKeysOriginal426Access.Require(ownState && errors.Count == 0 && differences.Count == 0
            && examined == MapKeysOriginal426Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
