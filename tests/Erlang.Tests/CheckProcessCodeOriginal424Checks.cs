using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class CheckProcessCodeOriginal424Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(CheckProcessCodeOriginal424Names.Output,
            System.Guid.NewGuid().ToString(CheckProcessCodeOriginal424Names.Guid)));
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
                RuntimeBootstrapCheckProcessCode423.Register(owner, registry);
                var declared = new BeamDependencySelection([CheckProcessCodeOriginal424Names.Runtime, CheckProcessCodeOriginal424Names.File, CheckProcessCodeOriginal424Names.Internal, CheckProcessCodeOriginal424Names.Subject, CheckProcessCodeOriginal424Names.Module]);
                PreparedBeamImage worker = registry.PrepareBeam(CheckProcessCodeOriginal424Access.Resource(CheckProcessCodeOriginal424Names.WorkerBeam), declared);
                CheckProcessCodeOriginal424Access.Require(registry.PublishPreparedBeam([worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        CheckProcessCodeOriginal424Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        CheckProcessCodeOriginal424Names.Module,
                        function,
                        new BitString(CheckProcessCodeOriginal424Access.Resource(CheckProcessCodeOriginal424Names.FirstImage)),
                        new BitString(CheckProcessCodeOriginal424Access.Resource(CheckProcessCodeOriginal424Names.SecondImage))
                    ));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        CheckProcessCodeOriginal424Names.Runtime,
                        CheckProcessCodeOriginal424Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, CheckProcessCodeOriginal424Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, CheckProcessCodeOriginal424Names.Actual + file + CheckProcessCodeOriginal424Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(CheckProcessCodeOriginal424Names.Run, CheckProcessCodeOriginal424Names.Values);
            await Capture(CheckProcessCodeOriginal424Names.Stacks, CheckProcessCodeOriginal424Names.StackValues);
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
        foreach (string file in new[] { CheckProcessCodeOriginal424Names.Values, CheckProcessCodeOriginal424Names.StackValues })
        {
            byte[] bytes = CheckProcessCodeOriginal424Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, CheckProcessCodeOriginal424Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, CheckProcessCodeOriginal424Names.Expected + file + CheckProcessCodeOriginal424Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == CheckProcessCodeOriginal424Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    CheckProcessCodeOriginal424Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    CheckProcessCodeOriginal424Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, CheckProcessCodeOriginal424Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = CheckProcessCodeOriginal424Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                ActualTwoCompiledHelperVersions = true,
                RealLoadDeletePurgeAndActiveSourceOwnership = true,
                PriorFailedOriginal1Preserved = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        CheckProcessCodeOriginal424Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == CheckProcessCodeOriginal424Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
