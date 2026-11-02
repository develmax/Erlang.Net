using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.DynamicApplyOriginal477Names;

namespace Erlang.Tests;

internal static class DynamicApplyOriginal477Checks
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
            using Stream input = typeof(DynamicApplyOriginal477Checks).Assembly.GetManifestResourceStream(Prefix + file)!;
            using var bytes = new MemoryStream();
            input.CopyTo(bytes);

            return bytes.ToArray();
        }

        using JsonDocument manifest = JsonDocument.Parse(Resource(Manifest));
        byte[] value = Resource(name);
        Require(Convert.ToHexString(SHA256.HashData(value)) == manifest.RootElement.GetProperty(ManifestFiles).GetProperty(name).GetString());

        return value;
    }

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            var dependencies = new BeamDependencySelection(Imports);
            PreparedBeamImage[] images = new[] { ListsImage, OdbcImage, ObserverImage }
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
                    Observer,
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
        byte[] expected = Read(Golden);
        Directory.CreateDirectory(Output);
        byte[] encoded = ExternalTermFormat.Encode(actual);
        File.WriteAllBytes(Path.Combine(Output, Managed), encoded);
        Require(Cons.Items(actual).Count() == Outcomes);
        Require(actual.Equals(ExternalTermFormat.Decode(expected)) && encoded.AsSpan().SequenceEqual(expected));
    }

    internal static async Task HostBindings()
    {
        foreach (bool asynchronous in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                var image = scope.Prepare(() => registry.PrepareBeam(Read(ObserverImage), new BeamDependencySelection(Imports)));

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                });
            }
            );
            Require(runtime.Modules.DeleteModule(Erts).Equals(Term.A(ErlangBooleanAtoms.True)));
            int calls = 0;
            runtime.Modules.Register(
                Erts,
                Apply,
                3,
                async (context, arguments) =>
            {
                if (asynchronous)
                    await Task.Yield();
                Require(arguments.Count == 3 && arguments[0].Equals(Term.I(0)) && arguments[2] is Nil);
                calls++;

                return Term.I(Marker);
            }
            );
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Term tail = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                Observer,
                Tail,
                Term.I(0),
                Term.I(0),
                Nil.Value
            ));
            Term fixedTail = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                Observer,
                Fixed,
                Term.A(Erts),
                Term.A(Apply),
                Term.I(0),
                Term.I(0),
                Nil.Value
            ));
            Require(tail.Equals(Term.I(Marker)) && fixedTail.Equals(Term.I(Marker)) && calls == 2);
        }
    }
}
