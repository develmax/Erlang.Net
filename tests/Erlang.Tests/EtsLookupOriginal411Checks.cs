using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class EtsLookupOriginal411Checks
{
    internal static async Task Run()
    {
        byte[] image = EtsLookupOriginal411Access.Resource(EtsLookupOriginal411Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(EtsLookupOriginal411Names.Runtime),
            RuntimeModuleSelection.Managed(EtsLookupOriginal411Names.Binary),
            RuntimeModuleSelection.Beam(EtsLookupOriginal411Names.Module, image,
                [EtsLookupOriginal411Names.Runtime, EtsLookupOriginal411Names.Binary])
        ]);
        bool missingProviderRefused = false;
        try
        {
            await using var missing = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            missingProviderRefused = true;
        }
        EtsLookupOriginal411Access.Require(missingProviderRefused);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            selection, RuntimeNativePrimitives.EtsStorageCore, CancellationToken.None
        );
        EtsLookupOriginal411Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.EtsStorageCore
            && runtime.NativePrimitives.Bindings.Count == EtsLookupOriginal411Names.NativeBodies
            && runtime.NativePrimitives.Bindings.All(binding => binding.Module == EtsLookupOriginal411Names.Ets
                && runtime.Modules.SourceOwner(binding.Module, binding.Function, binding.Arity) is null)
            && runtime.Modules.SourceOwner(EtsLookupOriginal411Names.Module, EtsLookupOriginal411Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == EtsLookupOriginal411Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 3
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == EtsLookupOriginal411Names.Module ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed)));
        string output = Path.Combine(EtsLookupOriginal411Names.ArtifactRoot,
            Guid.NewGuid().ToString(EtsLookupOriginal411Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();
        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, EtsLookupOriginal411Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, EtsLookupOriginal411Names.Runtime, EtsLookupOriginal411Names.Encode, value
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
        var actualValues = await Capture(EtsLookupOriginal411Names.Run,
            EtsLookupOriginal411Names.ActualValues, EtsLookupOriginal411Names.TypedActualValues);
        var actualStacks = await Capture(EtsLookupOriginal411Names.Stacks,
            EtsLookupOriginal411Names.ActualStacks, EtsLookupOriginal411Names.TypedActualStacks);
        // Both actual guest routes settle before any passive expectation decode.
        byte[] originalValues = EtsLookupOriginal411Access.Resource(EtsLookupOriginal411Names.ValuesFile);
        byte[] originalStacks = EtsLookupOriginal411Access.Resource(EtsLookupOriginal411Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, EtsLookupOriginal411Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, EtsLookupOriginal411Names.ExpectedStacks), originalStacks);
        File.WriteAllText(Path.Combine(output, EtsLookupOriginal411Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues)));
        File.WriteAllText(Path.Combine(output, EtsLookupOriginal411Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks)));
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(expectedValues, actualValues.Value,
                EtsLookupOriginal411Names.Run, EtsLookupOriginal411Names.Rows, differences, ref examined);
        if (actualStacks.Value is not null)
            equal += Profiles(expectedStacks, actualStacks.Value, differences, ref examined);
        bool wholeValues = expectedValues.Equals(actualValues.Value);
        bool wholeStacks = expectedStacks.Equals(actualStacks.Value);
        bool rawValues = actualValues.Bytes is not null && originalValues.AsSpan().SequenceEqual(actualValues.Bytes);
        bool rawStacks = actualStacks.Bytes is not null && originalStacks.AsSpan().SequenceEqual(actualStacks.Bytes);
        File.WriteAllText(Path.Combine(output, EtsLookupOriginal411Names.Receipt), JsonSerializer.Serialize(new
        {
            Expected = EtsLookupOriginal411Names.WholeRows, Examined = examined, Equal = equal,
            WholeValuesEqual = wholeValues, WholeStacksEqual = wholeStacks,
            WholeRawValuesEqual = rawValues, WholeRawStacksEqual = rawStacks,
            MissingProviderRefused = missingProviderRefused, NativeProviders = runtime.NativePrimitives,
            SourceImageSha256 = Convert.ToHexString(SHA256.HashData(image)),
            SourceImages = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256 }),
            Errors = errors, Differences = differences, FrameProjection = false,
            ProfileArity = 2, ProfileEnvelopeRewritten = false, WholeEts51Claim = false,
            ManagedBinaryExplicitlySelected = true, ZeroHeaderProjectionNativeCompared = false,
            FullLookupElementContractClaim = false, CompatibilityPromotion = false
        }));
        EtsLookupOriginal411Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsLookupOriginal411Names.WholeRows && equal == examined
            && wholeValues && wholeStacks && rawValues && rawStacks);
    }

    private static int Profiles(Term expected, Term actual, List<object> differences, ref int examined)
    {
        Term[] left = IoPrintableOriginal404Terms.List(expected, out bool originalProper);
        Term[] right = IoPrintableOriginal404Terms.List(actual, out bool actualProper);
        if (!originalProper || !actualProper || left.Length != EtsLookupOriginal411Names.ProfileCount
            || right.Length != EtsLookupOriginal411Names.ProfileCount)
            differences.Add(new { OriginalProper = originalProper, ActualProper = actualProper,
                OriginalProfiles = left.Length, ActualProfiles = right.Length });
        int equal = 0;
        for (int index = 0; index < Math.Max(left.Length, right.Length); index++)
        {
            Term? original = index < left.Length ? left[index] : null;
            Term? observed = index < right.Length ? right[index] : null;
            if (original is not TupleTerm { Items.Count: 2 } first || observed is not TupleTerm { Items.Count: 2 } second)
            {
                differences.Add(new { Profile = index, InvalidShape = true,
                    Original = original is null ? null : OwnedFunctionOriginal407Terms.Typed(original),
                    Actual = observed is null ? null : OwnedFunctionOriginal407Terms.Typed(observed) });
                continue;
            }
            if (!first.Items[0].Equals(second.Items[0]))
                differences.Add(new { Profile = index, HeaderEqual = false,
                    Original = OwnedFunctionOriginal407Terms.Typed(first), Actual = OwnedFunctionOriginal407Terms.Typed(second) });
            equal += IoPrintableOriginal404Comparison.Rows(first.Items[1], second.Items[1], index,
                EtsLookupOriginal411Names.Rows, differences, ref examined);
        }

        return equal;
    }
}
