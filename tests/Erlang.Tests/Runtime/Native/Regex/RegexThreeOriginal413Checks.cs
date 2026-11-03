using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class RegexThreeOriginal413Checks
{
    internal static Task Whole() => Compare(RegexThreeOriginal413Names.Suite);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(RegexThreeOriginal413Names.ArtifactRoot, suite.Prefix,
            Guid.NewGuid().ToString(RegexThreeOriginal413Names.GuidFormat));
        Directory.CreateDirectory(output);
        var errors = new List<object>();
        var differences = new List<object>();
        await using ProcessRuntime runtime = await Start(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        // Both actual routes run and are retained before any original comparison is attempted.
        Term? actualValues = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Run,
            output,
            RegexThreeOriginal413Names.ActualValues,
            RegexThreeOriginal413Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            RegexThreeOriginal413Names.ActualStacks,
            RegexThreeOriginal413Names.TypedStacks,
            errors
        );
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
        byte[] originalValueBytes = RegexThreeOriginal413Access.Read(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = RegexThreeOriginal413Access.Read(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = ExternalTermFormat.Decode(originalValueBytes);
        Term originalStacks = ExternalTermFormat.Decode(originalStackBytes);
        File.WriteAllBytes(Path.Combine(output, RegexThreeOriginal413Names.ExpectedValues), originalValueBytes);
        File.WriteAllBytes(Path.Combine(output, RegexThreeOriginal413Names.ExpectedStacks), originalStackBytes);
        File.WriteAllText(
            Path.Combine(output, RegexThreeOriginal413Names.ExpectedTypedValues),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalValues))
        );
        File.WriteAllText(
            Path.Combine(output, RegexThreeOriginal413Names.ExpectedTypedStacks),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalStacks))
        );
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += EtsKernelOriginal407Comparison.Rows(
                originalValues,
                actualValues,
                RegexThreeOriginal413Names.ValuesScope,
                suite.Rows,
                differences,
                ref examined
            );
        if (actualStacks is not null)
            equal += EtsKernelOriginal407Comparison.Profiles(
                originalStacks,
                actualStacks,
                suite.Rows,
                differences,
                ref examined
            );
        bool valuesEqual = actualValues is not null && originalValues.Equals(actualValues);
        bool stacksEqual = actualStacks is not null && originalStacks.Equals(actualStacks);
        bool rawValuesEqual = actualValues is not null && originalValueBytes.AsSpan().SequenceEqual(ExternalTermFormat.Encode(actualValues));
        bool rawStacksEqual = actualStacks is not null && originalStackBytes.AsSpan().SequenceEqual(ExternalTermFormat.Encode(actualStacks));
        File.WriteAllText(
            Path.Combine(output, RegexThreeOriginal413Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(RegexThreeOriginal413Access.Read(suite.Observer + LibraryDependencyOriginalNames.Beam))),
                ExpectedCompleteOutcomeRows = RegexThreeOriginal413Names.CompleteRows,
                ExaminedCompleteOutcomeRows = examined,
                WholeTypedEqualRows = equal,
                WholeValuesTermEqual = valuesEqual,
                WholeStacksTermEqual = stacksEqual,
                WholeValuesRawEqual = rawValuesEqual,
                WholeStacksRawEqual = rawStacksEqual,
                OriginalValuesSha256 = Convert.ToHexString(SHA256.HashData(originalValueBytes)),
                OriginalStacksSha256 = Convert.ToHexString(SHA256.HashData(originalStackBytes)),
                ActualValuesSha256 = actualValues is null ? null : Convert.ToHexString(SHA256.HashData(ExternalTermFormat.Encode(actualValues))),
                ActualStacksSha256 = actualStacks is null ? null : Convert.ToHexString(SHA256.HashData(ExternalTermFormat.Encode(actualStacks))),
                Differences = differences,
                CaptureErrors = errors,
                FrameFieldsProjected = false,
                ApiCompatibilityPromotion = false,
                OriginalRe8ImageLoaded = true,
                OriginalReImageSha256 = Convert.ToHexString(SHA256.HashData(RegexThreeOriginal413Access.Read(RegexThreeOriginal413Names.Module + LibraryDependencyOriginalNames.Beam))),
                OriginalExportInputsSha256 = Convert.ToHexString(SHA256.HashData(RegexThreeOriginal413Access.Read(RegexThreeOriginal413Names.ExportInputs))),
                GenuineSourceOwnedNativeBodies = RegexThreeOriginal413Names.NativeBodies
            })
        );
        if (errors.Count != 0 || differences.Count != 0 || examined != RegexThreeOriginal413Names.CompleteRows ||
            equal != RegexThreeOriginal413Names.CompleteRows || !valuesEqual || !stacksEqual || !rawValuesEqual || !rawStacksEqual)
            throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
    }

    // Genuine original whole re8. Re5 is a prerequisite; private3 are captured
    // by the source-qualified preparation route, never separately registered.
    internal static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite)
    {
        var runtime = new ProcessRuntime(2);
        try
        {
            await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
            await runtime.InitializeModulesAsync(Erlang.Otp.OtpModules.Register);
            var dependencies = new BeamDependencySelection(RegexThreeOriginal413Names.OriginalModules
                .Concat([RegexThreeOriginal413Names.Erlang, suite.Observer]));
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                runtime,
                RegexThreeOriginal413Names.OriginalModules.Select(module => RegexThreeOriginal413Access.Read(module + LibraryDependencyOriginalNames.Beam)),
                dependencies,
                (registry, images) =>
                {
                    if (images.Single(image => image.Name == RegexThreeOriginal413Names.Module).NativePrimitiveExports.Count != RegexThreeOriginal413Names.NativeBodies
                        || registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                        throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
                    foreach (PreparedBeamImage image in images)
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                                throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
                }
            );
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                runtime,
                [RegexThreeOriginal413Access.Read(suite.Observer + LibraryDependencyOriginalNames.Beam)],
                dependencies,
                (registry, images) =>
                {
                    PreparedBeamImage worker = images.Single();
                    if (worker.NativePrimitiveExports.Count != 0
                        || registry.PublishPreparedBeam(worker).Status != BeamPublicationStatus.Published
                        || !ReferenceEquals(registry.SourceOwner(suite.Observer, LibraryDependencyOriginalNames.Run, 1), worker)
                        || !ReferenceEquals(registry.SourceOwner(suite.Observer, LibraryDependencyOriginalNames.Stacks, 1), worker))
                        throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
                }
            );

            return runtime;
        }
        catch (Exception original)
        {
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    private static async Task<Term?> Capture(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        LibraryDependencyOriginalSuite suite,
        string function,
        string output,
        string etfFile,
        string typedFile,
        List<object> errors
    )
    {
        Term actual;
        try
        {
            // Legitimate immutable guest input generated by original re, not expected outcomes.
            // It contains no PID/ref/FUN resource. Never regenerate serialized bytes in our VM.
            Term exportedInput = ExternalTermFormat.Decode(RegexThreeOriginal413Access.Read(RegexThreeOriginal413Names.ExportInputs));
            actual = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                suite.Observer,
                function,
                exportedInput
            ))
                .WaitAsync(LibraryDependencyOriginalNames.Deadline);
        }
        catch (Exception error)
        {
            errors.Add(new
            {
                Route = function,
                Error = error.ToString(),
                ErlangClass = (error as ErlangException)?.ExceptionClass,
                CompleteReason = error is ErlangException failure ? IoPrintableOriginal404Terms.Typed(failure.Reason) : null,
                CompleteTrace = error is ErlangException trace ? IoPrintableOriginal404Terms.Typed(trace.StackTraceTerm) : null
            });

            return null;
        }
        try
        {
            File.WriteAllBytes(Path.Combine(output, etfFile), ExternalTermFormat.Encode(actual));
            File.WriteAllText(Path.Combine(output, typedFile), JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(actual)));
        }
        catch (Exception error)
        {
            errors.Add(new { Route = function, RetentionError = error.ToString() });
        }

        return actual;
    }
}
