using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsInfoOriginal422Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsInfoOriginal422Names.Output,
            System.Guid.NewGuid().ToString(EtsInfoOriginal422Names.Guid)));
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
                RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                PreparedBeamImage facet = EtsInfoOriginal422Preparation.Prepare(owner, registry);
                var declared = new BeamDependencySelection([EtsInfoOriginal422Names.Ets,
                    EtsInfoOriginal422Names.Runtime, EtsInfoOriginal422Names.Lists, EtsInfoOriginal422Names.File, EtsInfoOriginal422Names.Internal]);
                PreparedBeamImage lists = registry.PrepareBeam(
                    EtsInfoOriginal422Access.Resource(EtsInfoOriginal422Names.ListsBeam), declared);
                PreparedBeamImage inspection = registry.PrepareBeam(EtsInfoOriginal422Access.Resource(EtsInfoOriginal422Names.InternalBeam), declared);
                EtsInfoOriginal422Access.Require(inspection.Name == EtsInfoOriginal422Names.Internal
                    && inspection.NativePrimitiveExports.Count == EtsInfoOriginal422Names.InternalNatives
                    && inspection.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet().SetEquals(EtsInfoOriginal422Names.InternalSignatures));
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsInfoOriginal422Access.Resource(EtsInfoOriginal422Names.WorkerBeam), declared);
                EtsInfoOriginal422Access.Require(registry.PublishPreparedBeam([facet, lists, inspection, worker]).Status == BeamPublicationStatus.Published);
                EtsInfoOriginal422Preparation.AssertPublished(registry, facet);
                EtsInfoOriginal422Access.Require(registry.Exports.Count(export => export.Module == EtsInfoOriginal422Names.Internal) == EtsInfoOriginal422Names.InternalExports);
                foreach (var image in new[] { lists, inspection, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsInfoOriginal422Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsInfoOriginal422Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsInfoOriginal422Names.Runtime,
                        EtsInfoOriginal422Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsInfoOriginal422Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsInfoOriginal422Names.Actual + file + EtsInfoOriginal422Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsInfoOriginal422Names.Run, EtsInfoOriginal422Names.Values);
            await Capture(EtsInfoOriginal422Names.Stacks, EtsInfoOriginal422Names.StackValues);
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
        foreach (string file in new[] { EtsInfoOriginal422Names.Values, EtsInfoOriginal422Names.StackValues })
        {
            byte[] bytes = EtsInfoOriginal422Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsInfoOriginal422Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsInfoOriginal422Names.Expected + file + EtsInfoOriginal422Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsInfoOriginal422Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsInfoOriginal422Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsInfoOriginal422Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsInfoOriginal422Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsInfoOriginal422Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialEts = true,
                SourceDerivedPartialInternal = true,
                WholeOriginalInternalClaim = false,
                PhysicalNumericEqualityClaim = false,
                RequestedConcurrencyParityClaim = false,
                WholeOriginalEts51Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsInfoOriginal422Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsInfoOriginal422Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
