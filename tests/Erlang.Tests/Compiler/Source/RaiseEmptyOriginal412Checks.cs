using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class RaiseEmptyOriginal412Checks
{
    internal static async Task Run()
    {
        byte[] image = RaiseEmptyOriginal412Access.Resource(RaiseEmptyOriginal412Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(RaiseEmptyOriginal412Names.Runtime),
            RuntimeModuleSelection.Beam(RaiseEmptyOriginal412Names.Module, image,
                [RaiseEmptyOriginal412Names.Runtime])
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        RaiseEmptyOriginal412Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None
            && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.Modules.SourceOwner(RaiseEmptyOriginal412Names.Module, RaiseEmptyOriginal412Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == RaiseEmptyOriginal412Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 2
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == RaiseEmptyOriginal412Names.Runtime ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        string output = Path.Combine(RaiseEmptyOriginal412Names.ArtifactRoot,
            Guid.NewGuid().ToString(RaiseEmptyOriginal412Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();
        var baselineProcesses = runtime.ProcessSnapshot().ToHashSet();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, RaiseEmptyOriginal412Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    RaiseEmptyOriginal412Names.Runtime,
                    RaiseEmptyOriginal412Names.Encode,
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
        var actualValues = await Capture(RaiseEmptyOriginal412Names.Run,
            RaiseEmptyOriginal412Names.ActualValues, RaiseEmptyOriginal412Names.TypedActualValues);
        var actualStacks = await Capture(
            RaiseEmptyOriginal412Names.Stacks,
            RaiseEmptyOriginal412Names.ActualStacks,
            RaiseEmptyOriginal412Names.TypedActualStacks
        );
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
        byte[] originalValues = RaiseEmptyOriginal412Access.Resource(RaiseEmptyOriginal412Names.ValuesFile);
        byte[] originalStacks = RaiseEmptyOriginal412Access.Resource(RaiseEmptyOriginal412Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, RaiseEmptyOriginal412Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, RaiseEmptyOriginal412Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, RaiseEmptyOriginal412Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, RaiseEmptyOriginal412Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                RaiseEmptyOriginal412Names.Run,
                RaiseEmptyOriginal412Names.Rows,
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
            Path.Combine(output, RaiseEmptyOriginal412Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = RaiseEmptyOriginal412Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                NativeProviders = nativeSnapshot,
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(image)),
                SourceImages = selectedImages,
                Errors = errors,
                NoGuestChildrenRemain = noGuestChildrenRemain,
                ActorMailboxEmpty = actorMailboxEmpty,
                Differences = differences,
                FrameProjection = false,
                ProfileArity = 2,
                ProfileEnvelopeRewritten = false,
                WholeRaiseFamilyContractClaim = false,
                CompatibilityPromotion = false
            })
        );
        RaiseEmptyOriginal412Access.Require(noGuestChildrenRemain && actorMailboxEmpty
            && errors.Count == 0 && differences.Count == 0
            && examined == RaiseEmptyOriginal412Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != RaiseEmptyOriginal412Names.ProfileCount
            || right.Length != RaiseEmptyOriginal412Names.ProfileCount)
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
                RaiseEmptyOriginal412Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
