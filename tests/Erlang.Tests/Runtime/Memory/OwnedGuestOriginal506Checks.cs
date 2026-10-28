using Erlang.Differential;
using static Erlang.Tests.OwnedGuestOriginal506Names;

namespace Erlang.Tests;

internal static class OwnedGuestOriginal506Checks
{
    internal static async Task Run()
    {
        string output = Path.Combine(AppContext.BaseDirectory, Artifacts, OutputPrefix + Guid.NewGuid());
        var artifacts = new List<GeneratedModuleArtifact>();
        try
        {
            foreach (string name in SourceFiles)
            {
                string source = System.Text.Encoding.UTF8.GetString(Read(name));
                artifacts.Add(GeneratedModuleCompiler.Compile(source, name));
            }
            var failures = new List<Exception>();
            foreach (bool owned in new[] { false, true })
            {
                try
                {
                    await Observe(owned);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count != 0)
                throw new AggregateException(failures);

            async Task Observe(bool owned)
            {
                string directory = Path.Combine(output, owned ? Owned : Ordinary);
                Directory.CreateDirectory(directory);
                {
                    await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
                    foreach (var artifact in artifacts)
                        artifact.Definition.Register(runtime.Modules);
                    var images = BeamFiles.Select(name => runtime.Modules.PrepareBeam(
                        Read(name), new BeamDependencySelection(RegistryModules))).ToArray();
                    if (runtime.Modules.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                        throw new InvalidOperationException(PublicationFailure);
                    await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                    await actor.InvokeAsync(async context =>
                    {
                        Term observed = await runtime.Modules.Call(context, WorkerModule, EntryPoint);
                        Term witness = context.Dictionary[Term.A(WitnessKey)];
                        await Write(Raw, observed, false);
                        await Write(DeterministicRaw, observed, true);
                        await Write(Witnesses, witness, false);

                        return true;

                        async Task Write(string name, Term value, bool deterministic)
                        {
                            Term version = Term.Tuple(Term.A(MinorVersion), Term.I(2));
                            Term options = deterministic ? Term.List(Term.A(Deterministic), version) : Term.List(version);
                            var binary = (BitString)await runtime.Modules.Call(
                                context,
                                ErlangModule,
                                TermToBinary,
                                value,
                                options
                            );
                            await File.WriteAllBytesAsync(Path.Combine(directory, name), binary.ToArray());
                        }
                    });
                }
                // Compare only after actual host/runtime disposal has joined
                // every process, callback and payload owner from this profile.
                foreach (string name in new[] { Raw, DeterministicRaw })
                    if (!File.ReadAllBytes(Path.Combine(directory, name)).AsSpan().SequenceEqual(Read(name)))
                        throw new InvalidOperationException(Mismatch + directory + Path.DirectorySeparatorChar + name);
            }
        }
        finally
        {
            foreach (var artifact in artifacts)
                artifact.Dispose();
        }
    }

    private static byte[] Read(string name)
    {
        using var stream = typeof(OwnedGuestOriginal506Checks).Assembly.GetManifestResourceStream(ResourcePrefix + name)
            ?? throw new InvalidOperationException(MissingResource + name);
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }
}
