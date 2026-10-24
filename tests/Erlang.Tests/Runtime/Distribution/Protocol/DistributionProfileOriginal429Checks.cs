using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DistributionProfileOriginal429Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DistributionProfileOriginal429Names.Output,
            System.Guid.NewGuid().ToString(DistributionProfileOriginal429Names.Guid)));
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
                RuntimeBootstrapDistributionProfile429.Register(owner, registry);
                var declared = new BeamDependencySelection([DistributionProfileOriginal429Names.Runtime, DistributionProfileOriginal429Names.File, DistributionProfileOriginal429Names.Internal]);
                PreparedBeamImage facet = registry.PrepareBeam(DistributionProfileOriginal429Access.Resource(DistributionProfileOriginal429Names.FacetBeam), declared);
                DistributionProfileOriginal429Access.Require(facet.Name == DistributionProfileOriginal429Names.Internal
                    && facet.NativePrimitiveExports.Count == DistributionProfileOriginal429Names.NativeCount
                    && DistributionProfileOriginal429Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                PreparedBeamImage worker = registry.PrepareBeam(DistributionProfileOriginal429Access.Resource(DistributionProfileOriginal429Names.WorkerBeam), declared);
                DistributionProfileOriginal429Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DistributionProfileOriginal429Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                DistributionProfileOriginal429Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == DistributionProfileOriginal429Names.FacetExports);
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DistributionProfileOriginal429Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DistributionProfileOriginal429Names.Runtime,
                        DistributionProfileOriginal429Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DistributionProfileOriginal429Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DistributionProfileOriginal429Names.Actual + file + DistributionProfileOriginal429Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DistributionProfileOriginal429Names.Run, DistributionProfileOriginal429Names.Values);
            await Capture(DistributionProfileOriginal429Names.Stacks, DistributionProfileOriginal429Names.StackValues);
            await Capture(DistributionProfileOriginal429Names.ProfileFunction, DistributionProfileOriginal429Names.ProfileRecord);
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
        foreach (string file in new[] { DistributionProfileOriginal429Names.Values, DistributionProfileOriginal429Names.StackValues })
        {
            byte[] bytes = DistributionProfileOriginal429Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DistributionProfileOriginal429Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DistributionProfileOriginal429Names.Expected + file + DistributionProfileOriginal429Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DistributionProfileOriginal429Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DistributionProfileOriginal429Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DistributionProfileOriginal429Names.Rows,
                    differences,
                    ref examined
                );
        }
        byte[] nativeProfile = DistributionProfileOriginal429Access.Resource(DistributionProfileOriginal429Names.ProfileRecord);
        Term nativeRecord = OwnedFunctionOriginal407Terms.Decode(nativeProfile);
        File.WriteAllBytes(
            Path.Combine(output, DistributionProfileOriginal429Names.Expected + DistributionProfileOriginal429Names.ProfileRecord),
            nativeProfile
        );
        File.WriteAllText(
            Path.Combine(
                output,
                DistributionProfileOriginal429Names.Expected + DistributionProfileOriginal429Names.ProfileRecord + DistributionProfileOriginal429Names.Json
            ),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(nativeRecord))
        );
        bool ownRecordValid = captured.TryGetValue(DistributionProfileOriginal429Names.ProfileRecord, out var ownRecord)
            && ownRecord.Term is TupleTerm { Items.Count: 6 } record
            && record.Items[0].Equals(Term.A(DistributionProfileOriginal429Names.Record))
            && record.Items.Skip(1).All(value => value is Integer integer && integer.Value >= 0 && integer.Value <= ulong.MaxValue);
        File.WriteAllText(
            Path.Combine(output, DistributionProfileOriginal429Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DistributionProfileOriginal429Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedOneStubFacet = true,
                ExactOtpCapabilityBitmapEqualityClaim = false,
                ActualSourceOwnershipChecked = true,
                ActualOwnCapabilityProfile = true,
                WholeOriginalInternalImageClaim = false,
                OwnProfileRecordSchemaValid = ownRecordValid,
                NativeAndOwnProfileRecordsRetainedSeparately = true,
                ProfileRecordRawEqualityClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DistributionProfileOriginal429Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DistributionProfileOriginal429Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value) && ownRecordValid);
    }
}
