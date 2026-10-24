using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class SystemLoggerOriginal412Checks
{
    internal static async Task Run()
    {
        byte[] image = SystemLoggerOriginal412Access.Resource(SystemLoggerOriginal412Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(SystemLoggerOriginal412Names.Runtime),
            RuntimeModuleSelection.Beam(SystemLoggerOriginal412Names.Module, image,
                [SystemLoggerOriginal412Names.Runtime])
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        SystemLoggerOriginal412Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None
            && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.Modules.SourceOwner(SystemLoggerOriginal412Names.Module, SystemLoggerOriginal412Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == SystemLoggerOriginal412Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 2
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == SystemLoggerOriginal412Names.Runtime ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        string output = Path.Combine(SystemLoggerOriginal412Names.ArtifactRoot,
            Guid.NewGuid().ToString(SystemLoggerOriginal412Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var errors = new List<object>();
        var baselineProcesses = runtime.ProcessSnapshot().ToHashSet();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, SystemLoggerOriginal412Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    SystemLoggerOriginal412Names.Runtime,
                    SystemLoggerOriginal412Names.Encode,
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
            SystemLoggerOriginal412Names.Run,
            SystemLoggerOriginal412Names.ActualValues,
            SystemLoggerOriginal412Names.TypedActualValues
        );
        var actualStacks = await Capture(
            SystemLoggerOriginal412Names.Stacks,
            SystemLoggerOriginal412Names.ActualStacks,
            SystemLoggerOriginal412Names.TypedActualStacks
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
        byte[] originalValues = SystemLoggerOriginal412Access.Resource(SystemLoggerOriginal412Names.ValuesFile);
        byte[] originalStacks = SystemLoggerOriginal412Access.Resource(SystemLoggerOriginal412Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, SystemLoggerOriginal412Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, SystemLoggerOriginal412Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, SystemLoggerOriginal412Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, SystemLoggerOriginal412Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                SystemLoggerOriginal412Names.Run,
                SystemLoggerOriginal412Names.Rows,
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
            Path.Combine(output, SystemLoggerOriginal412Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = SystemLoggerOriginal412Names.WholeRows,
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
                WholeSystemLoggerRoutingContractClaim = false,
                CompatibilityPromotion = false
            })
        );
        SystemLoggerOriginal412Access.Require(noGuestChildrenRemain && actorMailboxEmpty
            && errors.Count == 0 && differences.Count == 0
            && examined == SystemLoggerOriginal412Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != SystemLoggerOriginal412Names.ProfileCount
            || right.Length != SystemLoggerOriginal412Names.ProfileCount)
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
                SystemLoggerOriginal412Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
