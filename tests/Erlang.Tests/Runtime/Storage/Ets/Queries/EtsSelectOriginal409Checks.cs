using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class EtsSelectOriginal409Checks
{
    internal static Task Whole() => Compare(EtsSelectOriginal409Names.Suite);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(EtsSelectOriginal409Names.ArtifactRoot, suite.Prefix,
            Guid.NewGuid().ToString(EtsSelectOriginal409Names.GuidFormat));
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
            EtsSelectOriginal409Names.ActualValues,
            EtsSelectOriginal409Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            EtsSelectOriginal409Names.ActualStacks,
            EtsSelectOriginal409Names.TypedStacks,
            errors
        );
        byte[] originalValueBytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = ExternalTermFormat.Decode(originalValueBytes);
        Term originalStacks = ExternalTermFormat.Decode(originalStackBytes);
        File.WriteAllBytes(Path.Combine(output, EtsSelectOriginal409Names.ExpectedValues), originalValueBytes);
        File.WriteAllBytes(Path.Combine(output, EtsSelectOriginal409Names.ExpectedStacks), originalStackBytes);
        File.WriteAllText(
            Path.Combine(output, EtsSelectOriginal409Names.ExpectedTypedValues),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalValues))
        );
        File.WriteAllText(
            Path.Combine(output, EtsSelectOriginal409Names.ExpectedTypedStacks),
            JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(originalStacks))
        );
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += EtsKernelOriginal407Comparison.Rows(
                originalValues,
                actualValues,
                EtsSelectOriginal409Names.ValuesScope,
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
            Path.Combine(output, EtsSelectOriginal409Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Observer + LibraryDependencyOriginalNames.Beam))),
                ExpectedCompleteOutcomeRows = EtsSelectOriginal409Names.CompleteRows,
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
                OriginalEts51ImageLoaded = false,
                ExplicitIsolatedOwnerBoundNativeBodies = EtsSelectOriginal409Names.NativeBodies
            })
        );
        if (errors.Count != 0 || differences.Count != 0 || examined != EtsSelectOriginal409Names.CompleteRows ||
            equal != EtsSelectOriginal409Names.CompleteRows || !valuesEqual || !stacksEqual || !rawValuesEqual || !rawStacksEqual)
            throw new InvalidOperationException(EtsSelectOriginal409Names.Failure);
    }

    // Explicit isolated native subset experiment. Never loads the original ETS
    // image or changes its unchanged complete51 primitive-capture gate.
    private static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite)
    {
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    Erlang.Otp.OtpModules.Register(registry);
                });
                var type = typeof(ProcessRuntime).Assembly.GetType(EtsSelectOriginal409Names.FacetType, true)!;
                var method = type.GetMethod(EtsSelectOriginal409Names.CreateBodies,
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
                var bodies = (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)method.Invoke(null, [runtime])!;
                if (bodies.Count != EtsSelectOriginal409Names.NativeBodies)
                    throw new InvalidOperationException(EtsSelectOriginal409Names.Failure);
                await runtime.InitializeModulesAsync(_ =>
                {
                    foreach (var entry in bodies)
                        registry.Register(
                            EtsSelectOriginal409Names.Module,
                            entry.Key.Function,
                            entry.Key.Arity,
                            entry.Value
                        );
                });
                var dependencies = new BeamDependencySelection([EtsSelectOriginal409Names.Module,
                                EtsSelectOriginal409Names.Erlang, suite.Observer]);
                PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(
                    LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Observer + LibraryDependencyOriginalNames.Beam),
                    dependencies
                ));

                return _ =>
                {
                    if (registry.PublishPreparedBeam([image]).Status != BeamPublicationStatus.Published ||
                    !ReferenceEquals(registry.SourceOwner(suite.Observer, LibraryDependencyOriginalNames.Run, 0), image))
                        throw new InvalidOperationException(EtsSelectOriginal409Names.Failure);
                };
            }
            );

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
