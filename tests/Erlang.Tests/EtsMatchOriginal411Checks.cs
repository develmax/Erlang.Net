using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class EtsMatchOriginal411Checks
{
    internal static async Task Run()
    {
        byte[] image = EtsMatchOriginal411Access.Resource(EtsMatchOriginal411Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(EtsMatchOriginal411Names.Runtime),
            RuntimeModuleSelection.Beam(EtsMatchOriginal411Names.Module, image,
                [EtsMatchOriginal411Names.Runtime])
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
        EtsMatchOriginal411Access.Require(missingProviderRefused);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            selection, RuntimeNativePrimitives.EtsStorageCore, CancellationToken.None
        );
        EtsMatchOriginal411Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.EtsStorageCore
            && runtime.NativePrimitives.Bindings.Count == EtsMatchOriginal411Names.NativeBodies
            && runtime.NativePrimitives.Bindings.All(binding => binding.Module == EtsMatchOriginal411Names.Ets
                && runtime.Modules.SourceOwner(binding.Module, binding.Function, binding.Arity) is null)
            && runtime.Modules.SourceOwner(EtsMatchOriginal411Names.Module, EtsMatchOriginal411Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == EtsMatchOriginal411Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 2
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == EtsMatchOriginal411Names.Runtime ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        string output = Path.Combine(EtsMatchOriginal411Names.ArtifactRoot,
            Guid.NewGuid().ToString(EtsMatchOriginal411Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, EtsMatchOriginal411Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    EtsMatchOriginal411Names.Runtime,
                    EtsMatchOriginal411Names.Encode,
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
        var actualValues = await Capture(EtsMatchOriginal411Names.Run,
            EtsMatchOriginal411Names.ActualValues, EtsMatchOriginal411Names.TypedActualValues);
        var actualStacks = await Capture(EtsMatchOriginal411Names.Stacks,
            EtsMatchOriginal411Names.ActualStacks, EtsMatchOriginal411Names.TypedActualStacks);
        // Both actual guest routes settle before any passive expectation decode.
        byte[] originalValues = EtsMatchOriginal411Access.Resource(EtsMatchOriginal411Names.ValuesFile);
        byte[] originalStacks = EtsMatchOriginal411Access.Resource(EtsMatchOriginal411Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, EtsMatchOriginal411Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, EtsMatchOriginal411Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, EtsMatchOriginal411Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, EtsMatchOriginal411Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                EtsMatchOriginal411Names.Run,
                EtsMatchOriginal411Names.Rows,
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
            Path.Combine(output, EtsMatchOriginal411Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsMatchOriginal411Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                MissingProviderRefused = missingProviderRefused,
                NativeProviders = runtime.NativePrimitives,
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(image)),
                SourceImages = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256 }),
                Errors = errors,
                Differences = differences,
                FrameProjection = false,
                ProfileArity = 2,
                ProfileEnvelopeRewritten = false,
                WholeEts51Claim = false,
                SignedZeroLiteralRawBitsCompared = true,
                OriginalCollectorPassed = false,
                NativeGatePassiveCorrectionOnly = true,
                FullMatchContractClaim = false,
                CompatibilityPromotion = false
            })
        );
        EtsMatchOriginal411Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsMatchOriginal411Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != EtsMatchOriginal411Names.ProfileCount
            || right.Length != EtsMatchOriginal411Names.ProfileCount)
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
                EtsMatchOriginal411Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
