using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class RegexNativeOriginal411Checks
{
    internal static Task Whole() => Compare(RegexNativeOriginal411Names.Suite);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(RegexNativeOriginal411Names.ArtifactRoot, suite.Prefix,
            Guid.NewGuid().ToString(RegexNativeOriginal411Names.GuidFormat));
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
            RegexNativeOriginal411Names.ActualValues,
            RegexNativeOriginal411Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            RegexNativeOriginal411Names.ActualStacks,
            RegexNativeOriginal411Names.TypedStacks,
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
        byte[] originalValueBytes = RegexNativeOriginal411Access.Read(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = RegexNativeOriginal411Access.Read(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = ExternalTermFormat.Decode(originalValueBytes);
        Term originalStacks = ExternalTermFormat.Decode(originalStackBytes);
        File.WriteAllBytes(Path.Combine(output, RegexNativeOriginal411Names.ExpectedValues), originalValueBytes);
        File.WriteAllBytes(Path.Combine(output, RegexNativeOriginal411Names.ExpectedStacks), originalStackBytes);
        File.WriteAllText(
            Path.Combine(output, RegexNativeOriginal411Names.ExpectedTypedValues),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalValues))
        );
        File.WriteAllText(
            Path.Combine(output, RegexNativeOriginal411Names.ExpectedTypedStacks),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalStacks))
        );
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += EtsKernelOriginal407Comparison.Rows(
                originalValues,
                actualValues,
                RegexNativeOriginal411Names.ValuesScope,
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
            Path.Combine(output, RegexNativeOriginal411Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(RegexNativeOriginal411Access.Read(suite.Observer + LibraryDependencyOriginalNames.Beam))),
                ExpectedCompleteOutcomeRows = RegexNativeOriginal411Names.CompleteRows,
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
                OriginalRe8ImageLoaded = false,
                ExplicitIsolatedOwnerBoundNativeBodies = RegexNativeOriginal411Names.NativeBodies
            })
        );
        if (errors.Count != 0 || differences.Count != 0 || examined != RegexNativeOriginal411Names.CompleteRows ||
            equal != RegexNativeOriginal411Names.CompleteRows || !valuesEqual || !stacksEqual || !rawValuesEqual || !rawStacksEqual)
            throw new InvalidOperationException(RegexNativeOriginal411Names.Failure);
    }

    // Explicit isolated native subset experiment. The complete original re8
    // image retains its native primitive admission requirements.
    private static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite)
    {
        var runtime = new ProcessRuntime();
        try
        {
            await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
            await runtime.InitializeModulesAsync(registry =>
            {
                Erlang.Otp.OtpModules.Register(registry);
                var dependencies = new BeamDependencySelection([RegexNativeOriginal411Names.Module,
                    RegexNativeOriginal411Names.Erlang, suite.Observer]);
                PreparedBeamImage image = registry.PrepareBeam(
                    RegexNativeOriginal411Access.Read(suite.Observer + LibraryDependencyOriginalNames.Beam),
                    dependencies
                );
                if (registry.PublishPreparedBeam([image]).Status != BeamPublicationStatus.Published ||
                    !ReferenceEquals(registry.SourceOwner(suite.Observer, LibraryDependencyOriginalNames.Run, 0), image))
                    throw new InvalidOperationException(RegexNativeOriginal411Names.Failure);
            });

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
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
