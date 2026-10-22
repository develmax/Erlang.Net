using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebuggerOriginal413Checks
{
    internal static Task Default() => Compare(DebuggerOriginal413Names.Default);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(DebuggerOriginal413Names.ArtifactRoot, suite.Prefix,
            Guid.NewGuid().ToString(DebuggerOriginal413Names.GuidFormat));
        Directory.CreateDirectory(output);
        var errors = new List<object>();
        var differences = new List<object>();
        await using ProcessRuntime runtime = await DebuggerOriginal413Resources.Start(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        // Both actual routes run and are retained before any original comparison is attempted.
        Term? actualValues = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Run,
            output,
            DebuggerOriginal413Names.ActualValues,
            DebuggerOriginal413Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            DebuggerOriginal413Names.ActualStacks,
            DebuggerOriginal413Names.TypedStacks,
            errors
        );
        byte[] originalValueBytes = DebuggerOriginal413Resources.Read(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = DebuggerOriginal413Resources.Read(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = ExternalTermFormat.Decode(originalValueBytes);
        Term originalStacks = ExternalTermFormat.Decode(originalStackBytes);
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                originalValues,
                actualValues,
                DebuggerOriginal413Names.ValuesScope,
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
            Path.Combine(output, DebuggerOriginal413Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                ExpectedCompleteOutcomeRows = DebuggerOriginal413Names.CompleteRows,
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
        if (errors.Count != 0 || differences.Count != 0 || examined != DebuggerOriginal413Names.CompleteRows ||
            equal != DebuggerOriginal413Names.CompleteRows || !valuesEqual || !stacksEqual)
            throw new InvalidOperationException(DebuggerOriginal413Names.Failure);
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
