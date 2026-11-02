using Erlang;
namespace Erlang.Tests;

internal static class OtpLiteral815Checks
{
    internal static async Task Run()
    {
        string root = Environment.GetEnvironmentVariable("LITERAL815_ROOT") ?? throw new InvalidOperationException();
        string profile = Environment.GetEnvironmentVariable("LITERAL815_PROFILE") ?? "ordinary";
        bool owned = profile switch
        {
            "ordinary" => false,
            "private-owned" => true,
            _ => throw new ArgumentException("Unknown profile")
        };
        await using var runtime = owned ? OwnedGuest506RuntimeFactory.Create() : new ProcessRuntime();
        bool actualMode = (bool)typeof(ProcessRuntime).GetProperty("OwnedTermsPilot506", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(runtime)!;
        if (actualMode != owned) throw new InvalidOperationException("Wrong actual VM profile");
        string setup = Path.Combine(Directory.GetParent(root)!.FullName, "original-library-assets06.json");
        using var metadata = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(setup));
        var selection = new BeamDependencySelection(metadata.RootElement.GetProperty("DeclaredModules").EnumerateArray().Select(x => x.GetString()!).ToArray());
        await runtime.InitializeModulesAsync(registry =>
       {
           RuntimeBootstrapDebugSharing425.Register(runtime, registry);
           RuntimeBootstrapAtomics.Register(runtime, registry);
       });
        await PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
            {
                var images = metadata.RootElement.GetProperty("Assets").EnumerateArray().Select(x =>
                {
                    string path = x.GetProperty("Path").GetString()!;
                    byte[] bytes = File.ReadAllBytes(path);
                    if (Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) != x.GetProperty("Sha256").GetString()) throw new InvalidDataException("Original library bytes changed");

                    return scope.Prepare(() => registry.PrepareBeam(bytes, selection));
                }).ToArray();

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    if (registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published) throw new InvalidOperationException("Original dependency publication refused");
                    foreach (var image in images)
                        foreach (var export in registry.Exports.Where(x => x.Module == image.Name))
                            if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)) throw new InvalidOperationException("Dependency backend changed");
                });
            }
        );
        await using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(root, "literal815.beam")), selection);
        if (runtime.Modules.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published) throw new InvalidOperationException("Publication refused");
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, "literal815", "run"));
        byte[] actual = ExternalTermFormat.Encode(result);
        File.WriteAllBytes(Path.Combine(root, "managed-" + profile + ".etf"), actual);
        Console.WriteLine(result);
        if (!actual.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(root, "original.etf")))) throw new InvalidOperationException("Complete original literal result differs");
    }
}
