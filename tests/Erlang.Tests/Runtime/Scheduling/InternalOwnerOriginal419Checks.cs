using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalOwnerOriginal419Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalOwnerOriginal419Names.Output,
            System.Guid.NewGuid().ToString(InternalOwnerOriginal419Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                owner,
                async (registry, scope) =>
            {
                await owner.InitializeModulesAsync(_ =>
                {
                    RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.PersistentTermCore);
                });
                PreparedBeamImage facet = InternalOwnerOriginal419Preparation.Prepare171(owner, registry, scope);
                var declared = new BeamDependencySelection([InternalOwnerOriginal419Names.Internal,
                                InternalOwnerOriginal419Names.Runtime, InternalOwnerOriginal419Names.Persistent, InternalOwnerOriginal419Names.File, InternalOwnerOriginal419Names.Module]);
                PreparedBeamImage library = scope.Prepare(() => registry.PrepareBeam(
                    InternalOwnerOriginal419Access.Resource(InternalOwnerOriginal419Names.PersistentBeam), declared));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(
                    InternalOwnerOriginal419Access.Resource(InternalOwnerOriginal419Names.WorkerBeam), declared));
                InternalOwnerOriginal419Access.Require(library.NativePrimitiveExports.Count == 7);

                return _ =>
                {
                    InternalOwnerOriginal419Access.Require(registry.PublishPreparedBeam([facet, library, worker]).Status == BeamPublicationStatus.Published);
                    InternalOwnerOriginal419Preparation.AssertPublished(registry, facet);
                    foreach (var image in new[] { library, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            InternalOwnerOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                };
            }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        InternalOwnerOriginal419Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalOwnerOriginal419Names.Runtime,
                        InternalOwnerOriginal419Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, InternalOwnerOriginal419Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, InternalOwnerOriginal419Names.Actual + file + InternalOwnerOriginal419Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(InternalOwnerOriginal419Names.Run, InternalOwnerOriginal419Names.Values);
            await Capture(InternalOwnerOriginal419Names.Stacks, InternalOwnerOriginal419Names.StackValues);
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
        foreach (string file in new[] { InternalOwnerOriginal419Names.Values, InternalOwnerOriginal419Names.StackValues })
        {
            byte[] bytes = InternalOwnerOriginal419Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, InternalOwnerOriginal419Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, InternalOwnerOriginal419Names.Expected + file + InternalOwnerOriginal419Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == InternalOwnerOriginal419Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    InternalOwnerOriginal419Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    InternalOwnerOriginal419Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, InternalOwnerOriginal419Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = InternalOwnerOriginal419Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialInternal = true,
                WholeOriginalInternal71Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        InternalOwnerOriginal419Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == InternalOwnerOriginal419Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
