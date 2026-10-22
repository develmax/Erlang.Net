using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class WritableReceiveOriginal411Checks
{
    internal static async Task Run()
    {
        byte[] image = WritableReceiveOriginal411Access.Resource(WritableReceiveOriginal411Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(WritableReceiveOriginal411Names.Runtime),
            RuntimeModuleSelection.Beam(WritableReceiveOriginal411Names.Module, image,
                [WritableReceiveOriginal411Names.Runtime])
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        WritableReceiveOriginal411Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None
            && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.Modules.SourceOwner(WritableReceiveOriginal411Names.Module, WritableReceiveOriginal411Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == WritableReceiveOriginal411Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 2
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == WritableReceiveOriginal411Names.Runtime ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        string output = Path.Combine(WritableReceiveOriginal411Names.ArtifactRoot,
            Guid.NewGuid().ToString(WritableReceiveOriginal411Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, WritableReceiveOriginal411Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    WritableReceiveOriginal411Names.Runtime,
                    WritableReceiveOriginal411Names.Encode,
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
            WritableReceiveOriginal411Names.Run,
            WritableReceiveOriginal411Names.ActualValues,
            WritableReceiveOriginal411Names.TypedActualValues
        );
        var actualStacks = await Capture(
            WritableReceiveOriginal411Names.Stacks,
            WritableReceiveOriginal411Names.ActualStacks,
            WritableReceiveOriginal411Names.TypedActualStacks
        );
        // Both actual guest routes settle before any passive expectation decode.
        byte[] originalValues = WritableReceiveOriginal411Access.Resource(WritableReceiveOriginal411Names.ValuesFile);
        byte[] originalStacks = WritableReceiveOriginal411Access.Resource(WritableReceiveOriginal411Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, WritableReceiveOriginal411Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, WritableReceiveOriginal411Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, WritableReceiveOriginal411Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, WritableReceiveOriginal411Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                WritableReceiveOriginal411Names.Run,
                WritableReceiveOriginal411Names.Rows,
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
            Path.Combine(output, WritableReceiveOriginal411Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = WritableReceiveOriginal411Names.WholeRows,
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
                WholePeerExecutionClaim = false,
                CompatibilityPromotion = false
            })
        );
        WritableReceiveOriginal411Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == WritableReceiveOriginal411Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != WritableReceiveOriginal411Names.ProfileCount
            || right.Length != WritableReceiveOriginal411Names.ProfileCount)
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
                WritableReceiveOriginal411Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
