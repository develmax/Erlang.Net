using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class IoPrintableOriginal404Checks
{
    internal static Task Default() => Compare(IoPrintableOriginal404Names.Default);

    internal static Task Latin1() => Compare(IoPrintableOriginal404Names.Latin1);

    internal static Task Unicode() => Compare(IoPrintableOriginal404Names.Unicode);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(IoPrintableOriginal404Names.ArtifactRoot, suite.Prefix,
            Guid.NewGuid().ToString(IoPrintableOriginal404Names.GuidFormat));
        Directory.CreateDirectory(output);
        var errors = new List<object>();
        var differences = new List<object>();
        await using ProcessRuntime runtime = await LibraryDependencyOriginalHostChecks.StartRetainedSuite(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        // Both actual routes run and are retained before any original comparison is attempted.
        Term? actualValues = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Run,
            output,
            IoPrintableOriginal404Names.ActualValues,
            IoPrintableOriginal404Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            IoPrintableOriginal404Names.ActualStacks,
            IoPrintableOriginal404Names.TypedStacks,
            errors
        );
        byte[] originalValueBytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = ExternalTermFormat.Decode(originalValueBytes);
        Term originalStacks = ExternalTermFormat.Decode(originalStackBytes);
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                originalValues,
                actualValues,
                IoPrintableOriginal404Names.ValuesScope,
                suite.Rows,
                differences,
                ref examined
            );
        if (actualStacks is not null)
            equal += IoPrintableOriginal404Comparison.Profiles(
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
            Path.Combine(output, IoPrintableOriginal404Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                ExpectedCompleteOutcomeRows = IoPrintableOriginal404Names.CompleteRows,
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
                ApiCompatibilityPromotion = false
            })
        );
        if (errors.Count != 0 || differences.Count != 0 || examined != IoPrintableOriginal404Names.CompleteRows ||
            equal != IoPrintableOriginal404Names.CompleteRows || !valuesEqual || !stacksEqual)
            throw new InvalidOperationException(IoPrintableOriginal404Names.Failure);
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
