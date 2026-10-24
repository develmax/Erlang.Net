using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class BootstrapSchedulerOriginal412Checks
{
    internal static async Task Run()
    {
        byte[] image = BootstrapSchedulerOriginal412Access.Resource(BootstrapSchedulerOriginal412Names.Beam);
        // Explicit opt-in native setter, followed by the actual ordinary source loader.
        // No default-provider or whole erts_internal source-image claim.
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(BootstrapSchedulerOriginal412Names.Runtime)
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        BootstrapSchedulerOriginal412Access.Require(!runtime.Modules.HasExport(
            BootstrapSchedulerOriginal412Names.NativeModule, BootstrapSchedulerOriginal412Names.NativeFunction, 1));
        RuntimeBootstrapScheduler.Register(runtime, runtime.Modules);
        bool nativeQualified = (bool)BeamBinaryHostAccess.Call(
            runtime.Modules,
            BootstrapSchedulerOriginal412Names.NativeEntryMethod,
            BootstrapSchedulerOriginal412Names.NativeModule,
            BootstrapSchedulerOriginal412Names.NativeFunction,
            1
        );
        BootstrapSchedulerOriginal412Access.Require(nativeQualified
            && runtime.Modules.SourceOwner(BootstrapSchedulerOriginal412Names.NativeModule,
                BootstrapSchedulerOriginal412Names.NativeFunction, 1) is null);
        var dependencies = new BeamDependencySelection([
            BootstrapSchedulerOriginal412Names.Runtime, BootstrapSchedulerOriginal412Names.NativeModule
        ]);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image, dependencies);
        BootstrapSchedulerOriginal412Access.Require(prepared.Name == BootstrapSchedulerOriginal412Names.Module
            && prepared.NativePrimitiveExports.Count == 0
            && prepared.RegistryModuleDependencies.SetEquals([
                BootstrapSchedulerOriginal412Names.Runtime, BootstrapSchedulerOriginal412Names.NativeModule])
            && !runtime.Modules.IsModuleLoaded(prepared.Name));
        BootstrapSchedulerOriginal412Access.Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        BootstrapSchedulerOriginal412Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None
            && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.LibrarySelection.Modules.Count == 1
            && runtime.LibrarySelection.Modules.All(module => module.Module == BootstrapSchedulerOriginal412Names.Runtime
                && module.Backend == RuntimeModuleBackend.Managed)
            && ReferenceEquals(runtime.Modules.SourceOwner(prepared.Name, BootstrapSchedulerOriginal412Names.Run, 0), prepared)
            && runtime.Modules.Exports.Where(value => value.Module == prepared.Name)
                .All(value => ReferenceEquals(runtime.Modules.SourceOwner(value.Module, value.Function, value.Arity), prepared)));
        string output = Path.Combine(BootstrapSchedulerOriginal412Names.ArtifactRoot,
            Guid.NewGuid().ToString(BootstrapSchedulerOriginal412Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();
        var baselineProcesses = runtime.ProcessSnapshot().ToHashSet();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, BootstrapSchedulerOriginal412Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    BootstrapSchedulerOriginal412Names.Runtime,
                    BootstrapSchedulerOriginal412Names.Encode,
                    value
                ));
                File.WriteAllBytes(Path.Combine(output, raw), bytes.ToArray());
                File.WriteAllText(Path.Combine(output, typed), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value)));

                return (value, bytes.ToArray());
            }
            catch (Exception error)
            {
                errors.Add(new { Function = function, Failure = error.ToString() });

                return (null, null);
            }
        }
        var actualValues = await Capture(
            BootstrapSchedulerOriginal412Names.Run,
            BootstrapSchedulerOriginal412Names.ActualValues,
            BootstrapSchedulerOriginal412Names.TypedActualValues
        );
        var actualStacks = await Capture(
            BootstrapSchedulerOriginal412Names.Stacks,
            BootstrapSchedulerOriginal412Names.ActualStacks,
            BootstrapSchedulerOriginal412Names.TypedActualStacks
        );
        bool setterStateRestored = !runtime.CaptureManagedSchedulerWallTime().Enabled;
        var nativeSnapshot = runtime.NativePrimitives;
        var selectedImages = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256 }).ToArray();
        bool noGuestChildrenRemain = baselineProcesses.SetEquals(runtime.ProcessSnapshot());
        bool actorMailboxEmpty = (await actor.Ready).Mailbox.Count == 0;
        try
        {
            await actor.DisposeAsync();
        }
        catch (Exception error)
        {
            errors.Add(new { ActorCleanup = error.ToString() });
        }
        try
        {
            await runtime.DisposeAsync();
        }
        catch (Exception error)
        {
            errors.Add(new { RuntimeCleanup = error.ToString() });
        }
        // Both actual routes and explicit cleanup settle before pure expectation decode.
        byte[] originalValues = BootstrapSchedulerOriginal412Access.Resource(BootstrapSchedulerOriginal412Names.ValuesFile);
        byte[] originalStacks = BootstrapSchedulerOriginal412Access.Resource(BootstrapSchedulerOriginal412Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, BootstrapSchedulerOriginal412Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, BootstrapSchedulerOriginal412Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, BootstrapSchedulerOriginal412Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, BootstrapSchedulerOriginal412Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                BootstrapSchedulerOriginal412Names.Run,
                BootstrapSchedulerOriginal412Names.Rows,
                differences,
                ref examined
            );
        if (actualStacks.Value is not null)
            equal += Profiles(
                expectedStacks,
                actualStacks.Value,
                differences,
                ref examined
            );
        bool wholeValues = expectedValues.Equals(actualValues.Value);
        bool wholeStacks = expectedStacks.Equals(actualStacks.Value);
        bool rawValues = actualValues.Bytes is not null && originalValues.AsSpan().SequenceEqual(actualValues.Bytes);
        bool rawStacks = actualStacks.Bytes is not null && originalStacks.AsSpan().SequenceEqual(actualStacks.Bytes);
        File.WriteAllText(
            Path.Combine(output, BootstrapSchedulerOriginal412Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = BootstrapSchedulerOriginal412Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                NativeProviders = nativeSnapshot,
                OptionalNativeSetterQualified = nativeQualified,
                SetterStateRestored = setterStateRestored,
                DefaultCoreRegistrationChanged = false,
                ExplicitSourceWorker = true,
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(image)),
                SourceImages = selectedImages,
                Errors = errors,
                NoGuestChildrenRemain = noGuestChildrenRemain,
                ActorMailboxEmpty = actorMailboxEmpty,
                Differences = differences,
                FrameProjection = false,
                ProfileArity = 2,
                ProfileEnvelopeRewritten = false,
                PhysicalSchedulerCounterEqualityClaim = false,
                CompatibilityPromotion = false
            })
        );
        BootstrapSchedulerOriginal412Access.Require(setterStateRestored && noGuestChildrenRemain && actorMailboxEmpty
            && errors.Count == 0 && differences.Count == 0
            && examined == BootstrapSchedulerOriginal412Names.WholeRows && equal == examined
            && wholeValues && wholeStacks && rawValues && rawStacks);
    }

    private static int Profiles(
        Term expected,
        Term actual,
        List<object> differences,
        ref int examined
    )
    {
        Term[] left = IoPrintableOriginal404Terms.List(expected, out bool originalProper);
        Term[] right = IoPrintableOriginal404Terms.List(actual, out bool actualProper);
        if (!originalProper || !actualProper || left.Length != BootstrapSchedulerOriginal412Names.ProfileCount
            || right.Length != BootstrapSchedulerOriginal412Names.ProfileCount)
            differences.Add(new
            {
                OriginalProper = originalProper,
                ActualProper = actualProper,
                OriginalProfiles = left.Length,
                ActualProfiles = right.Length
            });
        int equal = 0;
        for (int index = 0; index < Math.Max(left.Length, right.Length); index++)
        {
            Term? original = index < left.Length ? left[index] : null;
            Term? observed = index < right.Length ? right[index] : null;
            if (original is not TupleTerm { Items.Count: 2 } first || observed is not TupleTerm { Items.Count: 2 } second)
            {
                differences.Add(new
                {
                    Profile = index,
                    InvalidShape = true,
                    Original = original is null ? null : OwnedFunctionOriginal407Terms.Typed(original),
                    Actual = observed is null ? null : OwnedFunctionOriginal407Terms.Typed(observed)
                });
                continue;
            }
            if (!first.Items[0].Equals(second.Items[0]))
                differences.Add(new
                {
                    Profile = index,
                    HeaderEqual = false,
                    Original = OwnedFunctionOriginal407Terms.Typed(first),
                    Actual = OwnedFunctionOriginal407Terms.Typed(second)
                });
            equal += IoPrintableOriginal404Comparison.Rows(
                first.Items[1],
                second.Items[1],
                index,
                BootstrapSchedulerOriginal412Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
