using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamProtectedReceiveHostChecks
{
    internal static Task Run() => RunSubject(
        BeamProtectedReceiveHostNames.Module,
        Read,
        BeamProtectedReceiveHostNames.Cases,
        BeamProtectedReceiveHostNames.Profiles,
        []
    );

    internal static async Task RunSubject(
        string module,
        Func<string, byte[]> read,
        int valueCount,
        int profileCount,
        IReadOnlyList<string> dependencies
    )
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage[] companionImages = dependencies.Select(name => runtime.Modules.PrepareBeam(
            read(name + BeamProtectedReceiveHostNames.BeamExtension),
            new BeamDependencySelection([BeamProtectedReceiveHostNames.Erlang])
        )).ToArray();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            read(module + BeamProtectedReceiveHostNames.BeamExtension),
            new BeamDependencySelection([BeamProtectedReceiveHostNames.Erlang, .. dependencies])
        );
        Require(image.Name == module);
        PreparedBeamImage[] images = [.. companionImages, image];
        Require(runtime.Modules.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage selected in images)
            Require(runtime.Modules.Exports.Where(entry => entry.Module == selected.Name)
                .All(entry => ReferenceEquals(selected, runtime.Modules.SourceOwner(entry.Module, entry.Function, entry.Arity))));
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                var captured = new List<(Term Actual, byte[] Encoded, string Resource)>();
                foreach (var observation in new[]
                {
                    (Entry: BeamProtectedReceiveHostNames.Run, Resource: BeamProtectedReceiveHostNames.Values),
                    (Entry: BeamProtectedReceiveHostNames.RunStacks, Resource: BeamProtectedReceiveHostNames.Stacks)
                })
                {
                    Term actual = await runtime.Modules.Call(
                        context,
                        image.Name,
                        observation.Entry,
                        []
                    );
                    int count = observation.Entry == BeamProtectedReceiveHostNames.Run
                        ? Cons.Items(actual).Count()
                        : Cons.Items(actual).Sum(row => Cons.Items(((TupleTerm)row).Items[1]).Count());
                    Require(count == (observation.Entry == BeamProtectedReceiveHostNames.Run
                        ? valueCount : profileCount));
                    captured.Add((actual, ExternalTermFormat.Encode(actual,
                        new EtfEncodingOptions { FunctionOwner = runtime.InitializationPid }), observation.Resource));
                }
                foreach (var observation in captured)
                {
                    byte[] expected = read(observation.Resource);
                    Require(observation.Encoded.AsSpan().SequenceEqual(expected));
                    Require(observation.Actual.Equals(runtime.ImportExternalTerm(expected).Term));
                }
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(BeamProtectedReceiveHostNames.Deadline);
        Require((await actor.Completion.WaitAsync(BeamProtectedReceiveHostNames.Deadline))
            .Equals(Term.A(BeamProtectedReceiveHostNames.Normal)));
    }

    private static byte[] Read(string name)
    {
        using Stream input = typeof(BeamProtectedReceiveHostChecks).Assembly
            .GetManifestResourceStream(BeamProtectedReceiveHostNames.ResourcePrefix + name)
            ?? throw new InvalidDataException(BeamProtectedReceiveHostNames.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamProtectedReceiveHostNames.Digests[name]);

        return bytes;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(BeamProtectedReceiveHostNames.Failure);
    }
}
