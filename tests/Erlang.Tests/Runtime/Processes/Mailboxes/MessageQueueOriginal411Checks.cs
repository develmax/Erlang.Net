using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class MessageQueueOriginal411Checks
{
    internal static async Task Run()
    {
        byte[] image = MessageQueueOriginal411Access.Resource(MessageQueueOriginal411Names.Beam);
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(MessageQueueOriginal411Names.Runtime),
            RuntimeModuleSelection.Beam(MessageQueueOriginal411Names.Module, image,
                [MessageQueueOriginal411Names.Runtime])
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        MessageQueueOriginal411Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None
            && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.Modules.SourceOwner(MessageQueueOriginal411Names.Module, MessageQueueOriginal411Names.Run, 0) is PreparedBeamImage
            && runtime.LibrarySelection.Modules.Any(module => module.Module == MessageQueueOriginal411Names.Module
                && module.InputSha256 == Convert.ToHexString(SHA256.HashData(image)))
            && runtime.LibrarySelection.Modules.Count == 2
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == MessageQueueOriginal411Names.Runtime ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        string output = Path.Combine(MessageQueueOriginal411Names.ArtifactRoot,
            Guid.NewGuid().ToString(MessageQueueOriginal411Names.GuidFormat));
        Directory.CreateDirectory(output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term observedSystemDefault = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            MessageQueueOriginal411Names.Runtime,
            MessageQueueOriginal411Names.SystemInfo,
            Term.A(MessageQueueOriginal411Names.QueueData)
        ));
        Term observedActorDefault = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            MessageQueueOriginal411Names.Runtime,
            MessageQueueOriginal411Names.ProcessInfo,
            context.Self,
            Term.A(MessageQueueOriginal411Names.QueueData)
        ));
        MessageQueueOriginal411Access.Require(observedSystemDefault.Equals(Term.A(MessageQueueOriginal411Names.OnHeap))
            && observedActorDefault.Equals(Term.Tuple(Term.A(MessageQueueOriginal411Names.QueueData), Term.A(MessageQueueOriginal411Names.OnHeap))));
        var errors = new List<object>();

        async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
        {
            try
            {
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context, MessageQueueOriginal411Names.Module, function
                ));
                BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    MessageQueueOriginal411Names.Runtime,
                    MessageQueueOriginal411Names.Encode,
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
            MessageQueueOriginal411Names.Run,
            MessageQueueOriginal411Names.ActualValues,
            MessageQueueOriginal411Names.TypedActualValues
        );
        var actualStacks = await Capture(
            MessageQueueOriginal411Names.Stacks,
            MessageQueueOriginal411Names.ActualStacks,
            MessageQueueOriginal411Names.TypedActualStacks
        );
        // Both actual guest routes settle before any passive expectation decode.
        byte[] originalValues = MessageQueueOriginal411Access.Resource(MessageQueueOriginal411Names.ValuesFile);
        byte[] originalStacks = MessageQueueOriginal411Access.Resource(MessageQueueOriginal411Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, MessageQueueOriginal411Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, MessageQueueOriginal411Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, MessageQueueOriginal411Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, MessageQueueOriginal411Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                MessageQueueOriginal411Names.Run,
                MessageQueueOriginal411Names.Rows,
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
            Path.Combine(output, MessageQueueOriginal411Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = MessageQueueOriginal411Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                NativeProviders = runtime.NativePrimitives,
                ObservedSystemDefault = OwnedFunctionOriginal407Terms.Typed(observedSystemDefault),
                ObservedActorDefault = OwnedFunctionOriginal407Terms.Typed(observedActorDefault),
                DefaultPolicyForcedByTest = false,
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(image)),
                SourceImages = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256 }),
                Errors = errors,
                Differences = differences,
                FrameProjection = false,
                ProfileArity = 2,
                ProfileEnvelopeRewritten = false,
                PhysicalHeapOrGcClaim = false,
                CompatibilityPromotion = false
            })
        );
        MessageQueueOriginal411Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == MessageQueueOriginal411Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != MessageQueueOriginal411Names.ProfileCount
            || right.Length != MessageQueueOriginal411Names.ProfileCount)
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
                MessageQueueOriginal411Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
