using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.FrameOriginal481Names;

namespace Erlang.Tests;

internal static class FrameOriginal481Checks
{
    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }

    private static byte[] Read(string name)
    {
        byte[] Resource(string file)
        {
            using Stream input = typeof(FrameOriginal481Checks).Assembly.GetManifestResourceStream(Prefix + file)!;
            using var bytes = new MemoryStream();
            input.CopyTo(bytes);

            return bytes.ToArray();
        }

        using JsonDocument manifest = JsonDocument.Parse(Resource(Manifest));
        byte[] value = Resource(name);
        Require(Convert.ToHexString(SHA256.HashData(value)) == manifest.RootElement.GetProperty(ManifestFiles).GetProperty(name).GetString());

        return value;
    }

    internal static Task Context() => Capture(
        ContextObserver,
        ContextImage,
        ContextGolden,
        ContextOutcomes,
        ContextDirectory
    );

    internal static Task Record() => Capture(
        RecordObserver,
        RecordImage,
        RecordGolden,
        RecordOutcomes,
        RecordDirectory
    );

    internal static Task Tls() => Capture(
        TlsObserver,
        TlsImage,
        TlsGolden,
        TlsOutcomes,
        TlsDirectory
    );

    internal static async Task Admission()
    {
        await using var runtime = new ProcessRuntime();
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            var dependencies = new BeamDependencySelection(Imports);
            PreparedBeamImage[] images = AdmissionImages.Select(name => scope.Prepare(() => registry.PrepareBeam(Read(name), dependencies))).ToArray();

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                foreach (PreparedBeamImage image in images)
                    Require(registry.Exports.Where(export => export.Module == image.Name)
                    .All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
            });
        }
        );
    }

    private static async Task Capture(
        string observer,
        string imageName,
        string golden,
        int count,
        string directory
    )
    {
        await using var runtime = new ProcessRuntime();
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            var dependencies = new BeamDependencySelection(Imports);
            string[] names = observer == TlsObserver ? [ListsImage, TlsVersionImage, SslImage, imageName] : [imageName];
            PreparedBeamImage[] images = names
                .Select(name => scope.Prepare(() => registry.PrepareBeam(Read(name), dependencies))).ToArray();

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                foreach (var image in images)
                    Require(registry.Exports.Where(export => export.Module == image.Name)
                    .All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
            });
        }
        );
        var completed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                var outcome = (TupleTerm)await runtime.Modules.Call(
                    context,
                    observer,
                    Worker,
                    context.Self
                );
                completed.TrySetResult(outcome.Items[1]);
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        Term actual = await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        byte[] expected = Read(golden);
        string output = Path.Combine(Output, directory);
        Directory.CreateDirectory(output);
        byte[] encoded = ExternalTermFormat.Encode(actual);
        File.WriteAllBytes(Path.Combine(output, Managed), encoded);
        Require(Cons.Items(actual).Count() == count);
        Require(actual.Equals(ExternalTermFormat.Decode(expected)) && encoded.AsSpan().SequenceEqual(expected));
    }

}
