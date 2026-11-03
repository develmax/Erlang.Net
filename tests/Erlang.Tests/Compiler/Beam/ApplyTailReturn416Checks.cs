using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class ApplyTailReturn416Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ApplyTailReturn416Names.Failure);
    }

    private static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var stream = typeof(ApplyTailReturn416Checks).Assembly.GetManifestResourceStream(ApplyTailReturn416Names.Prefix + file)!;
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);

            return bytes.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(ApplyTailReturn416Names.Manifest))!;
        byte[] actual = Read(name);
        Require(Convert.ToHexString(SHA256.HashData(actual)) == hashes[name]);

        return actual;
    }

    internal static async Task Run()
    {
        foreach (bool asynchronous in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
                });
                var image = scope.Prepare(() => registry.PrepareBeam(
                    Resource(ApplyTailReturn416Names.Worker),
                    new BeamDependencySelection([ErlangModuleNames.Module, ApplyTailReturn416Names.Ets, ApplyTailReturn416Names.PrimFile])
                ));
                Require(image.Name == ApplyTailReturn416Names.Module && image.NativePrimitiveExports.Count == 0);

                return _ =>
                {
                    Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                    foreach (var export in registry.Exports.Where(e => e.Module == image.Name))
                        Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                };
            }
            );
            int calls = 0;
            _ = runtime.Modules.DeleteModule(ErlangModuleNames.Module);
            // Replace the REAL currently selected registry entry. No canonical cache,
            // fake getter, source-frame injection or native handler substitution occurs.
            runtime.Modules.Register(
                ErlangModuleNames.Module,
                ErlangModuleNames.Error,
                1,
                async (context, args) =>
            {
                if (asynchronous) await Task.Yield();
                Require(ReferenceEquals(context.Runtime, runtime) && args.Count == 1 && args[0].Equals(Term.A(ApplyTailReturn416Names.Reason)));
                calls++;

                return Term.I(ApplyTailReturn416Names.Marker);
            }
            );
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Term returned = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, ApplyTailReturn416Names.Module, ApplyTailReturn416Names.Function));
            Require(calls == 1 && returned.Equals(Term.I(ApplyTailReturn416Names.Marker)));
        }
    }
}
