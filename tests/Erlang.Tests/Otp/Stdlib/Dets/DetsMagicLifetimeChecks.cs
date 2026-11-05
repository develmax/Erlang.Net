using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class DetsMagicLifetimeChecks
{
    internal const string Test = "runtime/dets/magic-reference-gc-contracts";

    internal static async Task Run()
    {
        string root = DetsBackendChecks.Repository();
        string capture = Path.Combine(root, "tests/Erlang.Tests/Resources/DetsGcBackend");
        byte[] observer = File.ReadAllBytes(Path.Combine(capture, "dets_magic_cases.beam"));
        Term expected = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(capture, "original.etf")));
        await using var runtime = new ProcessRuntime();
        await EtsCompiled419Access.Initialize(runtime);
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(observer,
            new BeamDependencySelection(["dets_magic_cases", "erlang", "ets"]));
        await runtime.InitializeModulesAsync(registry =>
        {
            if (registry.PublishPreparedBeam(image).Status != BeamPublicationStatus.Published)
            {
                throw new InvalidOperationException("DETS magic lifetime observer publication failed");
            }
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term actual = await actor.InvokeAsync(async context =>
        {
            Term tag = runtime.MakeReference();
            _ = await runtime.Modules.Call(
                context,
                "dets_magic_cases",
                "run_worker",
                context.Self,
                tag,
                Nil.Value
            );
            var reply = await context.ReceiveAsync<TupleTerm>(value => value is TupleTerm { Items.Count: 2 } pair
                && pair.Items[0].Equals(tag) ? pair : null);

            return reply!.Items[1];
        });
        string output = Path.Combine(root, "artifacts/dets-acceptance", "Magic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, "actual.etf"), ExternalTermFormat.Encode(actual));
        File.WriteAllText(
            Path.Combine(output, "receipt.json"),
            JsonSerializer.Serialize(
                new
                {
                    Equal = actual.Equals(expected),
                    Expected = expected.ToString(),
                    Actual = actual.ToString(),
                    ObserverSha256 = Convert.ToHexString(SHA256.HashData(observer)),
                    RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)))
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (!actual.Equals(expected))
        {
            throw new InvalidOperationException("DETS magic reference lifetime differs: " + output);
        }
    }
}
