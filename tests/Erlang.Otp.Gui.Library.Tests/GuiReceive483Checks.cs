using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static Erlang.Otp.Gui.Library.Tests.GuiReceive483Names;

namespace Erlang.Otp.Gui.Library.Tests;

internal static class GuiReceive483Checks
{
    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }

    internal static byte[] Read(string name)
    {
        byte[] Resource(string file)
        {
            using Stream input = typeof(GuiReceive483Checks).Assembly.GetManifestResourceStream(Prefix + file)!;
            using var bytes = new MemoryStream();
            input.CopyTo(bytes);

            return bytes.ToArray();
        }

        using JsonDocument manifest = JsonDocument.Parse(Resource(Manifest));
        byte[] value = Resource(name);
        Require(Convert.ToHexString(SHA256.HashData(value)) == manifest.RootElement.GetProperty(ManifestFiles).GetProperty(name).GetString());

        return value;
    }

    private static string[] Imports => JsonSerializer.Deserialize<string[]>(Read(ImportsResource))!;

    private static void Publish(ModuleRegistry registry, string[] names)
    {
        var dependencies = new BeamDependencySelection(Imports);
        PreparedBeamImage[] images = names.Select(name => registry.PrepareBeam(Read(name), dependencies)).ToArray();
        Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage image in images)
        {
            var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
            Require(exports.Length != 0 && exports.All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
        }
    }

    internal static async Task Run(string[] args)
    {
        string output = args.Length == 1 ? Path.Combine(args[0], OutputDirectory) : Output;
        await Capture(
            Observer,
            [ObserverImage],
            Golden,
            Outcomes,
            output
        );
        Console.WriteLine(Completion);
    }

    private static async Task Capture(
        string observer,
        string[] imageNames,
        string golden,
        int count,
        string directory
    )
    {
        await using var runtime = new ProcessRuntime();
        await Publish171Async(runtime, imageNames);
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
        string output = directory;
        Directory.CreateDirectory(output);
        byte[] encoded = ExternalTermFormat.Encode(actual);
        File.WriteAllBytes(Path.Combine(output, Managed), encoded);
        Require(Cons.Items(actual).Count() == count);
        Require(actual.Equals(ExternalTermFormat.Decode(expected)) && encoded.AsSpan().SequenceEqual(expected));
    }

    private static async Task Publish171Async(ProcessRuntime runtime, string[] names)
    {
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            var dependencies = new BeamDependencySelection(Imports);
            PreparedBeamImage[] images = names.Select(name => scope.Prepare(() => registry.PrepareBeam(Read(name), dependencies))).ToArray();

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                foreach (PreparedBeamImage image in images)
                {
                    var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                    Require(exports.Length != 0 && exports.All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
                }
            });
        }
        );
    }
}
