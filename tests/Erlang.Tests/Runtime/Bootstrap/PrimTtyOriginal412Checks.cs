using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class PrimTtyOriginal412Checks
{
    internal static async Task Run(string output, string admission)
    {
        Directory.CreateDirectory(output);
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        (Term? Value, byte[]? Bytes) actualValues = (null, null), actualStacks = (null, null);
        object[] sourceOwners = [];
        try
        {
            runtime = await PrimTtyOriginal412Runtime.Create(admission);
            actor = new RuntimeNodeFoundationHostActor(runtime);
            ProcessRuntime owner = runtime;
            RuntimeNodeFoundationHostActor caller = actor;
            await caller.InvokeAsync(async context =>
            {
                // Real hosted configuration fixture: table exists, no kernel override.
                // Unchanged application/controller get_env reads its actual ETS default.
                Term table = await owner.Modules.Call(
                    context,
                    PrimTtyOriginal412Names.Ets,
                    PrimTtyOriginal412Names.New,
                    Term.A(PrimTtyOriginal412Names.AcTable),
                    Term.List(Term.A(PrimTtyOriginal412Names.Named),
                        Term.A(PrimTtyOriginal412Names.Public), Term.A(PrimTtyOriginal412Names.Set))
                );
                PrimTtyOriginal412Access.Require(table.Equals(Term.A(PrimTtyOriginal412Names.AcTable)));
                Term loaded = await owner.Modules.Call(context, PrimTtyOriginal412Names.Tty, PrimTtyOriginal412Names.Load);
                PrimTtyOriginal412Access.Require(loaded.Equals(Term.A(PrimTtyOriginal412Names.Ok)));

                return true;
            });

            async Task<(Term? Value, byte[]? Bytes)> Capture(string function, string raw, string typed)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context, PrimTtyOriginal412Names.Module, function));
                    BitString bytes = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        PrimTtyOriginal412Names.Runtime,
                        PrimTtyOriginal412Names.Encode,
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
            actualValues = await Capture(PrimTtyOriginal412Names.Run,
                PrimTtyOriginal412Names.ActualValues, PrimTtyOriginal412Names.TypedActualValues);
            actualStacks = await Capture(PrimTtyOriginal412Names.Stacks,
                PrimTtyOriginal412Names.ActualStacks, PrimTtyOriginal412Names.TypedActualStacks);
            sourceOwners = runtime.Modules.Exports.Where(export =>
                    export.Module == PrimTtyOriginal412Names.Tty || export.Module == PrimTtyOriginal412Names.Module)
                .Select(export => (object)new
                {
                    export.Module,
                    export.Function,
                    export.Arity,
                    SourceOwned = runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity) is PreparedBeamImage
                })
                .ToArray();
        }
        catch (Exception error)
        {
            errors.Add(new { PreparationOrExecution = error.ToString() });
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { ActorCleanup = error.ToString() });
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { RuntimeCleanup = error.ToString() });
                }
        }
        // Both real routes and cleanup settle before expectation decode; no ImportExternalTerm.
        byte[] originalValues = PrimTtyOriginal412Access.Resource(PrimTtyOriginal412Names.ValuesFile);
        byte[] originalStacks = PrimTtyOriginal412Access.Resource(PrimTtyOriginal412Names.StacksFile);
        Term expectedValues = OwnedFunctionOriginal407Terms.Decode(originalValues);
        Term expectedStacks = OwnedFunctionOriginal407Terms.Decode(originalStacks);
        File.WriteAllBytes(Path.Combine(output, PrimTtyOriginal412Names.ExpectedValues), originalValues);
        File.WriteAllBytes(Path.Combine(output, PrimTtyOriginal412Names.ExpectedStacks), originalStacks);
        File.WriteAllText(
            Path.Combine(output, PrimTtyOriginal412Names.TypedExpectedValues),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedValues))
        );
        File.WriteAllText(
            Path.Combine(output, PrimTtyOriginal412Names.TypedExpectedStacks),
            JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedStacks))
        );
        var differences = new List<object>();
        int examined = 0, equal = 0;
        if (actualValues.Value is not null)
            equal += IoPrintableOriginal404Comparison.Rows(
                expectedValues,
                actualValues.Value,
                PrimTtyOriginal412Names.Run,
                PrimTtyOriginal412Names.Rows,
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
        bool wholeValues = expectedValues.Equals(actualValues.Value), wholeStacks = expectedStacks.Equals(actualStacks.Value);
        bool rawValues = actualValues.Bytes is not null && originalValues.AsSpan().SequenceEqual(actualValues.Bytes);
        bool rawStacks = actualStacks.Bytes is not null && originalStacks.AsSpan().SequenceEqual(actualStacks.Bytes);
        File.WriteAllText(
            Path.Combine(output, PrimTtyOriginal412Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = PrimTtyOriginal412Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeValuesEqual = wholeValues,
                WholeStacksEqual = wholeStacks,
                WholeRawValuesEqual = rawValues,
                WholeRawStacksEqual = rawStacks,
                SourceOwners = sourceOwners,
                Errors = errors,
                Differences = differences,
                ExpectedValuesSha256 = Convert.ToHexString(SHA256.HashData(originalValues)),
                ExpectedStacksSha256 = Convert.ToHexString(SHA256.HashData(originalStacks)),
                SameCompiledWorkerAndOriginalPrimTty = true,
                ProfileArity = 2,
                OriginalRe8Selected = false,
                GenuineOptInRe5 = true,
                HostedEmptyAcTableFixture = true,
                KernelBootClaim = false,
                FrameProjection = false,
                ProfileEnvelopeRewritten = false,
                CompatibilityPromotion = false
            })
        );
        PrimTtyOriginal412Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == PrimTtyOriginal412Names.WholeRows && equal == examined
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
        if (!originalProper || !actualProper || left.Length != PrimTtyOriginal412Names.ProfileCount
            || right.Length != PrimTtyOriginal412Names.ProfileCount)
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
                PrimTtyOriginal412Names.Rows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
