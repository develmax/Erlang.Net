using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class EtsConcurrencyOriginal411Checks
{
    internal static async Task Run()
    {
        byte[] image = EtsConcurrencyOriginal411Access.Resource(EtsConcurrencyOriginal411Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(EtsConcurrencyOriginal411Names.Runtime),
            RuntimeModuleSelection.Beam(EtsConcurrencyOriginal411Names.Module, image,
                [EtsConcurrencyOriginal411Names.Runtime])
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
        EtsConcurrencyOriginal411Access.Require(missingProviderRefused);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            selection, RuntimeNativePrimitives.EtsStorageCore, CancellationToken.None
        );
        EtsConcurrencyOriginal411Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.EtsStorageCore
            && runtime.NativePrimitives.Bindings.Count == EtsConcurrencyOriginal411Names.NativeBodies
            && runtime.NativePrimitives.Bindings.All(binding => binding.Module == EtsConcurrencyOriginal411Names.Ets
                && runtime.Modules.SourceOwner(binding.Module, binding.Function, binding.Arity) is null)
            && runtime.Modules.SourceOwner(EtsConcurrencyOriginal411Names.Module, EtsConcurrencyOriginal411Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == EtsConcurrencyOriginal411Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 2
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == EtsConcurrencyOriginal411Names.Runtime ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        string output = Path.Combine(EtsConcurrencyOriginal411Names.ArtifactRoot,
            Guid.NewGuid().ToString(EtsConcurrencyOriginal411Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, EtsConcurrencyOriginal411Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    EtsConcurrencyOriginal411Names.Runtime,
                    EtsConcurrencyOriginal411Names.Encode,
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
            EtsConcurrencyOriginal411Names.Run,
            EtsConcurrencyOriginal411Names.ActualValues,
            EtsConcurrencyOriginal411Names.TypedActualValues
        );
        var actualStacks = await Capture(
            EtsConcurrencyOriginal411Names.Stacks,
            EtsConcurrencyOriginal411Names.ActualStacks,
            EtsConcurrencyOriginal411Names.TypedActualStacks
        );
        // Both actual guest routes settle before any passive expectation decode.
        byte[] originalValues = EtsConcurrencyOriginal411Access.Resource(EtsConcurrencyOriginal411Names.ValuesFile);
        byte[] originalStacks = EtsConcurrencyOriginal411Access.Resource(EtsConcurrencyOriginal411Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, EtsConcurrencyOriginal411Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, EtsConcurrencyOriginal411Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, EtsConcurrencyOriginal411Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, EtsConcurrencyOriginal411Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                EtsConcurrencyOriginal411Names.Run,
                EtsConcurrencyOriginal411Names.Rows,
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
            Path.Combine(output, EtsConcurrencyOriginal411Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsConcurrencyOriginal411Names.WholeRows,
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
                InfoOrPhysicalLockPerformanceClaim = false,
                CompatibilityPromotion = false
            })
        );
        EtsConcurrencyOriginal411Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsConcurrencyOriginal411Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != EtsConcurrencyOriginal411Names.ProfileCount
            || right.Length != EtsConcurrencyOriginal411Names.ProfileCount)
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
                EtsConcurrencyOriginal411Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
