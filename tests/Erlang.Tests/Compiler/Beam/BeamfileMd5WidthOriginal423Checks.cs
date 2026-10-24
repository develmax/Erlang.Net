using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class BeamfileMd5WidthOriginal423Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(BeamfileMd5WidthOriginal423Names.Output,
            System.Guid.NewGuid().ToString(BeamfileMd5WidthOriginal423Names.Guid)));
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
                RuntimeBootstrapBeamfileMd5423.Register(owner, registry);
                var declared = new BeamDependencySelection([BeamfileMd5WidthOriginal423Names.Runtime, BeamfileMd5WidthOriginal423Names.File, BeamfileMd5WidthOriginal423Names.Internal]);
                PreparedBeamImage worker = registry.PrepareBeam(BeamfileMd5WidthOriginal423Access.Resource(BeamfileMd5WidthOriginal423Names.WorkerBeam), declared);
                BeamfileMd5WidthOriginal423Access.Require(registry.PublishPreparedBeam([worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        BeamfileMd5WidthOriginal423Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        BeamfileMd5WidthOriginal423Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        BeamfileMd5WidthOriginal423Names.Runtime,
                        BeamfileMd5WidthOriginal423Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, BeamfileMd5WidthOriginal423Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, BeamfileMd5WidthOriginal423Names.Actual + file + BeamfileMd5WidthOriginal423Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(BeamfileMd5WidthOriginal423Names.Run, BeamfileMd5WidthOriginal423Names.Values);
            await Capture(BeamfileMd5WidthOriginal423Names.Stacks, BeamfileMd5WidthOriginal423Names.StackValues);
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
        foreach (string file in new[] { BeamfileMd5WidthOriginal423Names.Values, BeamfileMd5WidthOriginal423Names.StackValues })
        {
            byte[] bytes = BeamfileMd5WidthOriginal423Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, BeamfileMd5WidthOriginal423Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, BeamfileMd5WidthOriginal423Names.Expected + file + BeamfileMd5WidthOriginal423Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == BeamfileMd5WidthOriginal423Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    BeamfileMd5WidthOriginal423Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    BeamfileMd5WidthOriginal423Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, BeamfileMd5WidthOriginal423Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = BeamfileMd5WidthOriginal423Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                RawBeamfileReadIndependentOfRuntimeLinking = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        BeamfileMd5WidthOriginal423Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == BeamfileMd5WidthOriginal423Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
