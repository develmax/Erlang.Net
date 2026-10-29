using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class CountersOriginal421Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(CountersOriginal421Names.Output,
            System.Guid.NewGuid().ToString(CountersOriginal421Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        string? expectedPrefix = null;
        object? actualProfile = null;
        try
        {
            runtime = new ProcessRuntime(1);
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                PreparedBeamImage facet = CountersOriginal421Preparation.Prepare(owner, registry);
                var declared = new BeamDependencySelection([CountersOriginal421Names.Internal,
                    CountersOriginal421Names.Runtime, CountersOriginal421Names.File, CountersOriginal421Names.Module]);
                PreparedBeamImage worker = registry.PrepareBeam(
                    CountersOriginal421Access.Resource(CountersOriginal421Names.WorkerBeam), declared);
                CountersOriginal421Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                CountersOriginal421Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        CountersOriginal421Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;
            _ = await caller.InvokeAsync(async context =>
            {
                var profile = await CountersOriginal616Profile.Select(context);
                expectedPrefix = profile.Prefix;
                actualProfile = new
                {
                    profile.DebugCompiled,
                    profile.WordSize,
                    profile.Schedulers,
                    profile.Online,
                    profile.DebugHeader,
                    ExpectedResourcePrefix = profile.Prefix
                };

                return Nil.Value;
            });

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        CountersOriginal421Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        CountersOriginal421Names.Runtime,
                        CountersOriginal421Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, CountersOriginal421Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, CountersOriginal421Names.Actual + file + CountersOriginal421Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(CountersOriginal421Names.Run, CountersOriginal421Names.Values);
            await Capture(CountersOriginal421Names.Stacks, CountersOriginal421Names.StackValues);
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
        foreach (string file in new[] { CountersOriginal421Names.Values, CountersOriginal421Names.StackValues })
        {
            if (expectedPrefix is null) continue;
            byte[] bytes = CountersOriginal421Access.ProfileResource616(file, expectedPrefix);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, CountersOriginal421Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, CountersOriginal421Names.Expected + file + CountersOriginal421Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == CountersOriginal421Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    CountersOriginal421Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    CountersOriginal421Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, CountersOriginal421Names.Receipt),
            JsonSerializer.Serialize(new
            {
                ActualRuntimeProfile = actualProfile,
                Expected = CountersOriginal421Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialInternal = true,
                WholeOriginalInternal71Claim = false,
                ProfileArity = 5,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        CountersOriginal421Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == CountersOriginal421Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
