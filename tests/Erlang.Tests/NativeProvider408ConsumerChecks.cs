using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class NativeProvider408ConsumerChecks
{
    internal static async Task Run()
    {
        string output = Path.Combine(NativeProvider408Names.ArtifactRoot, Guid.NewGuid().ToString(NativeProvider408Names.GuidFormat));
        Directory.CreateDirectory(output);
        string[] modules = [NativeProvider408Names.Subject, NativeProvider408Names.Observer];
        foreach (string module in modules)
            File.WriteAllBytes(
                Path.Combine(output, module + NativeProvider408Names.Beam),
                NativeProvider408Access.Resource(module + NativeProvider408Names.Beam)
            );
        var document = new RuntimeLibraryManifestDocument(
            RuntimeLibraryManifest.CurrentVersion,
            modules.Select(module =>
            new RuntimeLibraryManifestModule(
                module,
                RuntimeModuleBackend.Beam,
                module + NativeProvider408Names.Beam,
                Convert.ToHexString(SHA256.HashData(NativeProvider408Access.Resource(module + NativeProvider408Names.Beam))),
                module == NativeProvider408Names.Observer
                    ? [NativeProvider408Names.Subject, NativeProvider408Names.RuntimeModule]
                    : [NativeProvider408Names.RuntimeModule]
            )).Append(new RuntimeLibraryManifestModule(
                NativeProvider408Names.RuntimeModule,
                RuntimeModuleBackend.Managed,
                null,
                null,
                []
            )).ToArray()
        );
        string manifest = Path.Combine(output, RuntimeLibraryManifest.FileName);
        File.WriteAllText(manifest, JsonSerializer.Serialize(document, RuntimeLibraryManifest.SerializationOptions));
        RuntimeLibrarySelection selection = RuntimeLibraryManifest.Load(manifest);
        bool noProviderRefused = false, missingEtsRefused = false;
        try
        {
            await using var missing = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            noProviderRefused = true;
        }
        try
        {
            await using var partial = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.PersistentTermCore, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            missingEtsRefused = true;
        }
        NativeProvider408Access.Require(noProviderRefused && missingEtsRefused);
        await using var runtime = await ProcessRuntime.CreateAsync(selection, NativeProvider408Names.Providers, CancellationToken.None);
        NativeProvider408Access.Both(runtime);
        NativeProvider408Access.Require(runtime.LibrarySelection.Modules.Count == modules.Length + 1
            && runtime.LibrarySelection.Modules.All(module => module.Backend ==
                (module.Module == NativeProvider408Names.RuntimeModule ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term actualValues = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
            NativeProvider408Names.Observer, NativeProvider408Names.Run));
        BitString actualValueBytes = await NativeProvider408Access.Encode(runtime, actor, actualValues);
        File.WriteAllBytes(Path.Combine(output, NativeProvider408Names.ActualValues), actualValueBytes.ToArray());
        Term actualStacks = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
            NativeProvider408Names.Observer, NativeProvider408Names.RunStacks));
        BitString actualStackBytes = await NativeProvider408Access.Encode(runtime, actor, actualStacks);
        File.WriteAllBytes(Path.Combine(output, NativeProvider408Names.ActualStacks), actualStackBytes.ToArray());
        // Decode expectations only after both actual guest routes and encoders settled.
        byte[] expectedValueBytes = NativeProvider408Access.Resource(NativeProvider408Names.Values);
        byte[] expectedStackBytes = NativeProvider408Access.Resource(NativeProvider408Names.Stacks);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(expectedValueBytes);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(expectedStackBytes);
        File.WriteAllBytes(Path.Combine(output, NativeProvider408Names.ExpectedValues), expectedValueBytes);
        File.WriteAllBytes(Path.Combine(output, NativeProvider408Names.ExpectedStacks), expectedStackBytes);
        File.WriteAllText(
            Path.Combine(output, NativeProvider408Names.TypedActualValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(actualValues))
        );
        File.WriteAllText(
            Path.Combine(output, NativeProvider408Names.TypedActualStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(actualStacks))
        );
        File.WriteAllText(
            Path.Combine(output, NativeProvider408Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, NativeProvider408Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0;
        int equal = IoPrintableOriginal404Comparison.Rows(
            expectedValues,
            actualValues,
            NativeProvider408Names.Run,
            NativeProvider408Names.ConsumerValueRows,
            differences,
            ref examined
        );
        equal += IoPrintableOriginal404Comparison.Profiles(
            expectedStacks,
            actualStacks,
            NativeProvider408Names.ConsumerValueRows,
            differences,
            ref examined
        );
        bool rawValues = expectedValueBytes.AsSpan().SequenceEqual(actualValueBytes.ToArray());
        bool rawStacks = expectedStackBytes.AsSpan().SequenceEqual(actualStackBytes.ToArray());
        bool wholeValues = expectedValues.Equals(actualValues), wholeStacks = expectedStacks.Equals(actualStacks);
        File.WriteAllText(
            Path.Combine(output, NativeProvider408Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = NativeProvider408Names.ConsumerWholeRows,
                Examined = examined,
                TypedEqualRows = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                ActualNativeProviders = runtime.NativePrimitives,
                SourceImages = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256 }),
                Differences = differences,
                OriginalAll23PersistentCohortReplaced = false,
                FrameProjection = false,
                CompatibilityPromotion = false
            })
        );
        NativeProvider408Access.Require(examined == NativeProvider408Names.ConsumerWholeRows && equal == examined
            && differences.Count == 0 && wholeValues && wholeStacks && rawValues && rawStacks);
    }
}
