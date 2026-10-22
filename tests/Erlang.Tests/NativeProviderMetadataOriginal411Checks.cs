using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class NativeProviderMetadataOriginal411Checks
{
    internal static async Task Run()
    {
        byte[] image = NativeProviderMetadataOriginal411Access.Resource(NativeProviderMetadataOriginal411Names.Beam);
        // Ordinary explicit source loader. Metadata helpers are non-native;
        // strict factory static-import validation is intentionally unchanged.
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(NativeProviderMetadataOriginal411Names.Runtime)
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            selection, NativeProvider408Names.Providers, CancellationToken.None);
        var dependencies = new BeamDependencySelection([
            NativeProviderMetadataOriginal411Names.Runtime,
            NativeProvider408Names.Ets, NativeProvider408Names.Module
        ]);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image, dependencies);
        NativeProviderMetadataOriginal411Access.Require(prepared.Name == NativeProviderMetadataOriginal411Names.Module
            && prepared.NativePrimitiveExports.Count == 0 && !runtime.Modules.IsModuleLoaded(prepared.Name));
        NativeProviderMetadataOriginal411Access.Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        NativeProvider408Access.Both(runtime);
        NativeProviderMetadataOriginal411Access.Require(runtime.NativePrimitives.Bindings.Count == NativeProviderMetadata411Names.NativeCount
            && runtime.LibrarySelection.Modules.Count == 1
            && runtime.LibrarySelection.Modules.All(module => module.Module == NativeProviderMetadataOriginal411Names.Runtime
                && module.Backend == RuntimeModuleBackend.Managed)
            && runtime.Modules.Exports.Where(value => value.Module == prepared.Name)
                .All(value => ReferenceEquals(runtime.Modules.SourceOwner(value.Module, value.Function, value.Arity), prepared)));
        string output = Path.Combine(
            NativeProviderMetadataOriginal411Names.ArtifactRoot,
            Guid.NewGuid().ToString(NativeProviderMetadataOriginal411Names.GuidFormat)
        );
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, NativeProviderMetadataOriginal411Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    NativeProviderMetadataOriginal411Names.Runtime,
                    NativeProviderMetadataOriginal411Names.Encode,
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
            NativeProviderMetadataOriginal411Names.Run,
            NativeProviderMetadataOriginal411Names.ActualValues,
            NativeProviderMetadataOriginal411Names.TypedActualValues
        );
        var actualStacks = await Capture(
            NativeProviderMetadataOriginal411Names.Stacks,
            NativeProviderMetadataOriginal411Names.ActualStacks,
            NativeProviderMetadataOriginal411Names.TypedActualStacks
        );
        // Both actual guest routes settle before any passive expectation decode.
        byte[] originalValues = NativeProviderMetadataOriginal411Access.Resource(NativeProviderMetadataOriginal411Names.ValuesFile);
        byte[] originalStacks = NativeProviderMetadataOriginal411Access.Resource(NativeProviderMetadataOriginal411Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, NativeProviderMetadataOriginal411Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, NativeProviderMetadataOriginal411Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, NativeProviderMetadataOriginal411Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, NativeProviderMetadataOriginal411Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                NativeProviderMetadataOriginal411Names.Run,
                NativeProviderMetadataOriginal411Names.Rows,
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
            Path.Combine(output, NativeProviderMetadataOriginal411Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = NativeProviderMetadataOriginal411Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                NativeProviders = runtime.NativePrimitives,
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(image)),
                SourceImages = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256 }),
                Errors = errors,
                Differences = differences,
                FrameProjection = false,
                ProfileArity = 2,
                ProfileEnvelopeRewritten = false,
                OrdinaryExplicitSourcePublication = true,
                StrictFactoryStaticImportWaiver = false,
                HelpersNativeRelabelled = false,
                ActualNativeBindings = runtime.NativePrimitives.Bindings.Count,
                NativeHelperExports = NativeProviderMetadata411Names.MetadataCount,
                WholeEtsOrPersistentModuleExecutionClaim = false,
                CrossVmExportsMd5AssemblyEqualityClaim = false,
                CompatibilityPromotion = false
            })
        );
        NativeProviderMetadataOriginal411Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == NativeProviderMetadataOriginal411Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != NativeProviderMetadataOriginal411Names.ProfileCount
            || right.Length != NativeProviderMetadataOriginal411Names.ProfileCount)
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
                NativeProviderMetadataOriginal411Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
