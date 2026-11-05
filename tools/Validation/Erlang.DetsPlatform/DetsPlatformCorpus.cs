using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal sealed class DetsPlatformCorpus
{
    private const string Module = "dets_port_cases";
    private readonly string capture;
    private readonly string repository;
    private readonly string output;

    internal DetsPlatformCorpus(string repository, string output)
    {
        this.repository = repository;
        this.output = output;
        capture = Path.Combine(repository, "tests/Erlang.Tests/Resources/DetsBackend");
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(capture, "provenance.json")));
        if (provenance.RootElement.GetProperty("Commit").GetString() != "ad05823719d77c8faee87348ea39513d4e2f99c5")
            throw new InvalidDataException("DETS corpus OTP source pin differs");
        foreach (var (property, name) in new[] { ("ObserverSourceSha256", Module + ".erl"), ("ObserverBeamSha256", Module + ".beam"), ("CorpusSha256", "original.etf"), ("ErrorsSha256", "errors.etf") })
            if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(capture, name)))) != provenance.RootElement.GetProperty(property).GetString())
                throw new InvalidDataException("DETS corpus input identity differs: " + name);
    }

    internal PreparedBeamImage Prepare(ProcessRuntime runtime, BeamDependencySelection dependencies)
        => runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(capture, Module + ".beam")), dependencies);

    internal async ValueTask Execute(ProcessRuntime runtime, ProcessContext context, string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (string file in Directory.EnumerateFiles(Path.Combine(capture, "legacy")))
            File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
        foreach (bool errors in new[] { false, true })
        {
            Term tag = runtime.MakeReference();
            Term[] arguments = errors ? [context.Self, tag] : [context.Self, tag, Term.String(directory.Replace('\\', '/'))];
            _ = await runtime.Modules.Call(
                context,
                Module,
                errors ? "error_worker" : "run_worker",
                arguments
            );
            var reply = await context.ReceiveAsync<TupleTerm>(term => term is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(tag) ? pair : null);
            Term actual = reply!.Items[1];
            string name = errors ? "errors" : "original";
            byte[] expected = File.ReadAllBytes(Path.Combine(capture, name + ".etf"));
            File.WriteAllBytes(Path.Combine(directory, name + "-actual.etf"), ExternalTermFormat.Encode(actual));
            Term expectedTerm = ExternalTermFormat.Decode(expected);
            if (!errors) expectedTerm = DetsPlatformNativeExpectation.ForHost(expectedTerm, repository, output);
            File.WriteAllBytes(Path.Combine(directory, name + "-expected-host.etf"), ExternalTermFormat.Encode(expectedTerm));
            if (!actual.Equals(expectedTerm))
                throw new InvalidOperationException("DETS pinned corpus differs: " + name);
        }
        File.WriteAllText(
            Path.Combine(directory, "receipt.json"),
            "{\"Passed\":true,\"WholePrimaryScenarios\":21,\"CompleteErrorStacksEqual\":true}\n"
        );
    }
}
