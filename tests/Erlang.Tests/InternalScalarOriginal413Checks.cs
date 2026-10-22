using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class InternalScalarOriginal413Checks
{
    internal static Task Whole() => Compare(InternalScalarOriginal413Names.Suite);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(
            InternalScalarOriginal413Names.ArtifactRoot,
            suite.Prefix,
            Guid.NewGuid().ToString(InternalScalarOriginal413Names.GuidFormat)
        );
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
            InternalScalarOriginal413Names.ActualValues,
            InternalScalarOriginal413Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            InternalScalarOriginal413Names.ActualStacks,
            InternalScalarOriginal413Names.TypedStacks,
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
        byte[] originalValueBytes = InternalScalarOriginal413Access.Read(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = InternalScalarOriginal413Access.Read(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = ExternalTermFormat.Decode(originalValueBytes);
        Term originalStacks = ExternalTermFormat.Decode(originalStackBytes);
        File.WriteAllBytes(Path.Combine(output, InternalScalarOriginal413Names.ExpectedValues), originalValueBytes);
        File.WriteAllBytes(Path.Combine(output, InternalScalarOriginal413Names.ExpectedStacks), originalStackBytes);
        File.WriteAllText(
            Path.Combine(output, InternalScalarOriginal413Names.ExpectedTypedValues),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalValues))
        );
        File.WriteAllText(
            Path.Combine(output, InternalScalarOriginal413Names.ExpectedTypedStacks),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalStacks))
        );
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += EtsKernelOriginal407Comparison.Rows(
                originalValues,
                actualValues,
                InternalScalarOriginal413Names.ValuesScope,
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
            Path.Combine(output, InternalScalarOriginal413Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(InternalScalarOriginal413Access.Read(suite.Observer + LibraryDependencyOriginalNames.Beam))),
                ExpectedCompleteOutcomeRows = InternalScalarOriginal413Names.CompleteRows,
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
                OriginalErtsInternal71ImageLoaded = false,
                OriginalListsAndBinaryImageSelected = true,
                AdditionalOptInNativeBodies = InternalScalarOriginal413Names.NativeBodies
            })
        );
        if (errors.Count != 0 || differences.Count != 0 || examined != InternalScalarOriginal413Names.CompleteRows ||
            equal != InternalScalarOriginal413Names.CompleteRows || !valuesEqual || !stacksEqual || !rawValuesEqual || !rawStacksEqual)
            throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);
    }

    // Explicit real native4 profile; unchanged original71 remains unselected.
    private static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite)
    {
        var runtime = new ProcessRuntime(2);
        try
        {
            RuntimeBootstrapInternalScalars413.Register(runtime, runtime.Modules);
            foreach (var signature in InternalScalarOriginal413Names.NativeSignatures)
                if (!(bool)BeamBinaryHostAccess.Call(
                    runtime.Modules,
                    InternalScalarOriginal413Names.NativeEntryMethod,
                    InternalScalarOriginal413Names.NativeModule,
                    signature.Function,
                    signature.Arity
                )
                    || runtime.Modules.SourceOwner(InternalScalarOriginal413Names.NativeModule, signature.Function, signature.Arity) is not null)
                    throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);
            await runtime.InitializeModulesAsync(registry =>
            {
                Erlang.Otp.OtpModules.Register(registry);
                var dependencies = new BeamDependencySelection(InternalScalarOriginal413Names.OriginalModules
                    .Concat([InternalScalarOriginal413Names.Erlang, InternalScalarOriginal413Names.NativeModule, suite.Observer]));
                PreparedBeamImage[] images = InternalScalarOriginal413Names.OriginalModules.Select(module => registry.PrepareBeam(
                    InternalScalarOriginal413Access.Read(module + LibraryDependencyOriginalNames.Beam), dependencies
                )).ToArray();
                if (registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                    throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);
                foreach (PreparedBeamImage image in images)
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                            throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);
                PreparedBeamImage worker = registry.PrepareBeam(
                    InternalScalarOriginal413Access.Read(suite.Observer + LibraryDependencyOriginalNames.Beam), dependencies
                );
                if (worker.NativePrimitiveExports.Count != 0 || registry.PublishPreparedBeam(worker).Status != BeamPublicationStatus.Published
                    || !ReferenceEquals(registry.SourceOwner(suite.Observer, LibraryDependencyOriginalNames.Run, 0), worker)
                    || !ReferenceEquals(registry.SourceOwner(suite.Observer, LibraryDependencyOriginalNames.Stacks, 0), worker)
                    || runtime.NativePrimitives.Selection != RuntimeNativePrimitives.None || runtime.NativePrimitives.Bindings.Count != 0)
                    throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);
            });

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
            actual = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, suite.Observer, function))
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
