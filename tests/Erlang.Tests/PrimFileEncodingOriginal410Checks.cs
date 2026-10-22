using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class PrimFileEncodingOriginal410Checks
{
    internal static Task Whole() => Compare(PrimFileEncodingOriginal410Names.Suite);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        string output = Path.Combine(
            PrimFileEncodingOriginal410Names.ArtifactRoot,
            suite.Prefix,
            Guid.NewGuid().ToString(PrimFileEncodingOriginal410Names.GuidFormat)
        );
        Directory.CreateDirectory(output);
        var errors = new List<object>();
        var differences = new List<object>();
        await using ProcessRuntime runtime = await Start(suite, output);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        // Both actual routes run and are retained before any original comparison is attempted.
        Term? actualValues = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Run,
            output,
            PrimFileEncodingOriginal410Names.ActualValues,
            PrimFileEncodingOriginal410Names.TypedValues,
            errors
        );
        Term? actualStacks = await Capture(
            runtime,
            actor,
            suite,
            LibraryDependencyOriginalNames.Stacks,
            output,
            PrimFileEncodingOriginal410Names.ActualStacks,
            PrimFileEncodingOriginal410Names.TypedStacks,
            errors
        );
        byte[] originalValueBytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
        byte[] originalStackBytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
        // Pure decode AFTER guest capture: expected terms cannot change this runtime's node roots/state.
        Term originalValues = OwnedFunctionOriginal407Terms.Decode(originalValueBytes);
        Term originalStacks = OwnedFunctionOriginal407Terms.Decode(originalStackBytes);
        File.WriteAllBytes(Path.Combine(output, PrimFileEncodingOriginal410Names.ExpectedValues), originalValueBytes);
        File.WriteAllBytes(Path.Combine(output, PrimFileEncodingOriginal410Names.ExpectedStacks), originalStackBytes);
        File.WriteAllText(
            Path.Combine(output, PrimFileEncodingOriginal410Names.ExpectedTypedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(originalValues))
        );
        File.WriteAllText(
            Path.Combine(output, PrimFileEncodingOriginal410Names.ExpectedTypedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(originalStacks))
        );
        int examined = 0, equal = 0;
        if (actualValues is not null)
            equal += OwnedFunctionOriginal407Comparison.Rows(
                originalValues,
                actualValues,
                PrimFileEncodingOriginal410Names.ValuesScope,
                suite.Rows,
                differences,
                ref examined
            );
        if (actualStacks is not null)
            equal += OwnedFunctionOriginal407Comparison.Profiles(
                originalStacks,
                actualStacks,
                suite.Rows,
                differences,
                ref examined
            );
        bool valuesEqual = actualValues is not null && originalValues.Equals(actualValues);
        bool stacksEqual = actualStacks is not null && originalStacks.Equals(actualStacks);
        byte[]? actualValueBytes = actualValues is null ? null : File.ReadAllBytes(Path.Combine(output, PrimFileEncodingOriginal410Names.ActualValues));
        byte[]? actualStackBytes = actualStacks is null ? null : File.ReadAllBytes(Path.Combine(output, PrimFileEncodingOriginal410Names.ActualStacks));
        bool rawValuesEqual = actualValueBytes is not null && originalValueBytes.AsSpan().SequenceEqual(actualValueBytes);
        bool rawStacksEqual = actualStackBytes is not null && originalStackBytes.AsSpan().SequenceEqual(actualStackBytes);
        File.WriteAllText(
            Path.Combine(output, PrimFileEncodingOriginal410Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = suite.Prefix,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                OriginalPrimFileImageSha256 = Convert.ToHexString(SHA256.HashData(LibraryDependencyOriginalHostChecks.ReadRetainedResource(PrimFileEncodingOriginal410Names.ModuleName + LibraryDependencyOriginalNames.Beam))),
                OriginalObserverImageSha256 = Convert.ToHexString(SHA256.HashData(LibraryDependencyOriginalHostChecks.ReadRetainedResource(suite.Observer + LibraryDependencyOriginalNames.Beam))),
                ActualPrimFileNativeCount = ((PreparedBeamImage)runtime.Modules.SourceOwner(PrimFileEncodingOriginal410Names.ModuleName, PrimFileEncodingOriginal410Names.ToNative, 1)!).NativePrimitiveExports.Count,
                ExpectedCompleteOutcomeRows = PrimFileEncodingOriginal410Names.CompleteRows,
                ExaminedCompleteOutcomeRows = examined,
                WholeTypedEqualRows = equal,
                WholeValuesTermEqual = valuesEqual,
                WholeStacksTermEqual = stacksEqual,
                WholeValuesRawEqual = rawValuesEqual,
                WholeStacksRawEqual = rawStacksEqual,
                OriginalValuesSha256 = Convert.ToHexString(SHA256.HashData(originalValueBytes)),
                OriginalStacksSha256 = Convert.ToHexString(SHA256.HashData(originalStackBytes)),
                ActualValuesSha256 = actualValueBytes is null ? null : Convert.ToHexString(SHA256.HashData(actualValueBytes)),
                ActualStacksSha256 = actualStackBytes is null ? null : Convert.ToHexString(SHA256.HashData(actualStackBytes)),
                Differences = differences,
                CaptureErrors = errors,
                FrameFieldsProjected = false,
                ApiCompatibilityPromotion = false
            })
        );
        if (errors.Count != 0 || differences.Count != 0 || examined != PrimFileEncodingOriginal410Names.CompleteRows ||
            equal != PrimFileEncodingOriginal410Names.CompleteRows || !valuesEqual || !stacksEqual || !rawValuesEqual || !rawStacksEqual)
            throw new InvalidOperationException(PrimFileEncodingOriginal410Names.Failure);
    }

    private static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite, string output)
    {
        try
        {
            return await LibraryDependencyOriginalHostChecks.StartRetainedSuite(suite);
        }
        catch (Exception error)
        {
            var details = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in error.Data)
                details.Add(entry.Key.ToString()!, entry.Value?.ToString() ?? string.Empty);
            File.WriteAllText(
                Path.Combine(output, PrimFileEncodingOriginal410Names.AdmissionFailure),
                JsonSerializer.Serialize(new { Error = error.ToString(), Details = details })
            );

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
                CompleteReason = error is ErlangException failure ? OwnedFunctionOriginal407Terms.Typed(failure.Reason) : null,
                CompleteTrace = error is ErlangException trace ? OwnedFunctionOriginal407Terms.Typed(trace.StackTraceTerm) : null
            });

            return null;
        }
        try
        {
            var encoded = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                PrimFileEncodingOriginal410Names.RuntimeModule,
                PrimFileEncodingOriginal410Names.Encode,
                actual
            ));
            File.WriteAllBytes(Path.Combine(output, etfFile), encoded.ToArray());
            File.WriteAllText(Path.Combine(output, typedFile), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(actual)));
        }
        catch (Exception error)
        {
            errors.Add(new { Route = function, RetentionError = error.ToString() });
        }

        return actual;
    }
}
