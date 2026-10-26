using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugSharingOriginal425Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugSharingOriginal425Names.Output,
            System.Guid.NewGuid().ToString(DebugSharingOriginal425Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        var ownRaw = new Dictionary<string, bool>();
        var ownOrdinals = new Dictionary<string, Dictionary<string, int>>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = await DebugSharing425Access.SourceRuntime();
            ProcessRuntime owner = runtime;
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugSharingOriginal425Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugSharingOriginal425Names.Runtime,
                        DebugSharingOriginal425Names.Encode,
                        value
                    ));
                    if (file is DebugSharingOriginal425Names.Values or DebugSharingOriginal425Names.StackValues)
                    {
                        Term originalExpected = OwnedFunctionOriginal407Terms.Decode(DebugSharingOriginal425Access.Resource(file));
                        var ownExpected = await caller.InvokeAsync(_ => Task.FromResult(AtomMapWire494Expected.Encode(originalExpected)));
                        ownRaw[file] = ownExpected.Bytes.AsSpan().SequenceEqual(wire.ToArray());
                        ownOrdinals[file] = ownExpected.Indices;
                        File.WriteAllBytes(Path.Combine(output, AtomMapWire494Names.OwnPrefix + file), ownExpected.Bytes);
                    }
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugSharingOriginal425Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugSharingOriginal425Names.Actual + file + DebugSharingOriginal425Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugSharingOriginal425Names.Run, DebugSharingOriginal425Names.Values);
            await Capture(DebugSharingOriginal425Names.Stacks, DebugSharingOriginal425Names.StackValues);
            await Capture("sharing", "sharing.etf");
            await Capture("layouts", "layouts.etf");
        }
        catch (Exception error)
        {
            errors.Add(new { Preparation = error.ToString() });
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
        // Decode expectations only after both independent routes and real owner cleanup.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        foreach (string file in new[] { DebugSharingOriginal425Names.Values, DebugSharingOriginal425Names.StackValues })
        {
            byte[] bytes = DebugSharingOriginal425Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugSharingOriginal425Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugSharingOriginal425Names.Expected + file + DebugSharingOriginal425Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugSharingOriginal425Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugSharingOriginal425Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugSharingOriginal425Names.Rows,
                    differences,
                    ref examined
                );
        }
        bool sharingEqual = false;
        object? layoutEvidence = null;
        try
        {
            byte[] expected = DebugSharingOriginal425Access.Resource("sharing.etf");
            Term expectedTerm = OwnedFunctionOriginal407Terms.Decode(expected);
            File.WriteAllBytes(Path.Combine(output, "expected-sharing.etf"), expected);
            File.WriteAllText(
                Path.Combine(output, "expected-sharing.etf.json"),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expectedTerm))
            );
            sharingEqual = captured.TryGetValue("sharing.etf", out var sharing) && expectedTerm.Equals(sharing.Term)
                && expected.AsSpan().SequenceEqual(sharing.Bytes);
            byte[] expectedLayout = DebugSharingOriginal425Access.Resource("layouts.etf");
            Term layout = OwnedFunctionOriginal407Terms.Decode(expectedLayout);
            File.WriteAllBytes(Path.Combine(output, "expected-layouts.etf"), expectedLayout);
            File.WriteAllText(Path.Combine(output, "expected-layouts.etf.json"), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(layout)));
            bool present = captured.TryGetValue("layouts.etf", out var actualLayout);
            layoutEvidence = new
            {
                Present = present,
                Held = true,
                SourcePhysicalLayoutNumericEqualityClaim = false,
                ExactTermsEqual = present && layout.Equals(actualLayout.Term),
                RawEqual = present && expectedLayout.AsSpan().SequenceEqual(actualLayout.Bytes),
                Expected = OwnedFunctionOriginal407Terms.Typed(layout),
                Actual = present ? OwnedFunctionOriginal407Terms.Typed(actualLayout.Term) : null
            };
        }
        catch (Exception error)
        {
            errors.Add(new { SharingAndHeldLayout = error.ToString() });
        }
        File.WriteAllText(
            Path.Combine(output, DebugSharingOriginal425Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugSharingOriginal425Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                OwnAtomOrdinalWholeRawEtf = ownRaw,
                OwnAtomOrdinals = ownOrdinals,
                OriginalRawMapOrderDifferencesRetained = true,
                Errors = errors,
                Differences = differences,
                SharingWholeTermAndRawEqual = sharingEqual,
                PhysicalLayouts = layoutEvidence,
                SourceDerivedPartialDebug = true,
                WholeOriginalDebugClaim = false,
                PhysicalNumericEqualityClaim = false,
                OriginalCompilerVmRerunFromConsumer = false,
                ActualSettledOriginalRequiredByMaterializer = true,
                LogicalWordLayoutProfile = "Own immutable Term graph logical64; physical map/binary/global-empty partitions held",
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugSharingOriginal425Access.Require(sharingEqual && errors.Count == 0 && differences.Count == 0
            && examined == DebugSharingOriginal425Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && ownRaw.Count >= raw.Count && ownRaw.Values.All(value => value));
    }
}
