using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugSharing425Access
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugSharing425Names.Failure);
    }

    internal static bool Same(ProcessContext context, Term first, Term second)
    {
        return (bool)typeof(ProcessRuntime).GetMethod(DebugSharing425Names.SameCore, Members)!
            .Invoke(context.Runtime, [context, new Term[] { first, second }])!;
    }

    internal static TupleTerm Keys(MapTerm map) => (TupleTerm)typeof(MapTerm).GetProperty("FlatKeys426", Members)!.GetValue(map)!;

    internal static object Backing(BitString binary) => typeof(BitString).GetField("backing", Members)!.GetValue(binary)!;

    internal static object Resource(ReferenceTerm reference) => reference.GetType().GetProperty("Entry", Members)!.GetValue(reference)!;

    internal static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            DebugSharing425Names.Module,
            function,
            arguments
        );

    internal static byte[] Fixture(string name)
    {
        byte[] Read(string file)
        {
            using Stream source = typeof(DebugSharing425Access).Assembly.GetManifestResourceStream(DebugSharing425Names.Prefix + file)!;
            using var output = new MemoryStream();
            source.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugSharing425Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes[name] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static async Task<ProcessRuntime> SourceRuntime(bool privateProfile = false)
    {
        var runtime = privateProfile ? OwnedGuest506RuntimeFactory.Create() : new ProcessRuntime(1);
        try
        {
            await PrepareSourceRuntime(runtime);

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

    internal static async Task PrepareSourceRuntime(ProcessRuntime runtime)
    {
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
        {
            await runtime.InitializeModulesAsync(_ =>
            {
                {
                    RuntimeBootstrapDebugSharing425.Register(runtime, registry);
                }
                {
                    RuntimeBootstrapAtomics.Register(runtime, registry);
                }
            });
            var declared = new BeamDependencySelection(["erlang", "prim_file", "binary", "erts_debug", "erts_internal", "atomics", "lists", "maps"]);
            var helpers = new[] { "atomics", "lists", "maps" }.Select(module => scope.Prepare(() => registry.PrepareBeam(AtomicsHost413Access.Resource(module + ".beam"), declared))).ToArray();
            PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(Fixture("erts_debug.beam"), declared));
            Require(facet.NativePrimitiveExports.Count == 4);
            foreach (var signature in new[] { new BeamFunctionSignature("flat_size", 1), new BeamFunctionSignature("size_shared", 1), new BeamFunctionSignature("copy_shared", 2), new BeamFunctionSignature("same", 2) })
                Require(facet.NativePrimitiveExports.Contains(signature));
            PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(Fixture(DebugSharing425Names.Worker + ".beam"), declared));

            return _ =>
            {
                Require(registry.PublishPreparedBeam(helpers.Concat(new[] { facet, worker }).ToArray()).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            };
        }
        );
    }

}
