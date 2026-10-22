using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamRaiseTraceHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Read(BeamRaiseTraceHostNames.Beam),
            new BeamDependencySelection([BeamRaiseTraceHostNames.Erlang]));
        Require(image.Name == BeamRaiseTraceHostNames.Module);
        Require(runtime.Modules.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);
        Require(runtime.Modules.Exports.Where(entry => entry.Module == image.Name)
            .All(entry => ReferenceEquals(image, runtime.Modules.SourceOwner(entry.Module, entry.Function, entry.Arity))));
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                var captured = new List<(Term Actual, byte[] Encoded, string Resource)>();
                foreach (var observation in new[]
                {
                    (Entry: BeamRaiseTraceHostNames.Run, Resource: BeamRaiseTraceHostNames.Values),
                    (Entry: BeamRaiseTraceHostNames.RunStacks, Resource: BeamRaiseTraceHostNames.Stacks)
                })
                {
                    Term actual = await runtime.Modules.Call(
                        context,
                        image.Name,
                        observation.Entry,
                        []
                    );
                    int count = observation.Entry == BeamRaiseTraceHostNames.Run
                        ? Cons.Items(actual).Count()
                        : Cons.Items(actual).Sum(row => Cons.Items(((TupleTerm)row).Items[1]).Count());
                    Require(count == (observation.Entry == BeamRaiseTraceHostNames.Run
                        ? BeamRaiseTraceHostNames.Cases : BeamRaiseTraceHostNames.Profiles));
                    captured.Add((actual, ExternalTermFormat.Encode(actual,
                        new EtfEncodingOptions { FunctionOwner = runtime.InitializationPid }), observation.Resource));
                }
                foreach (var observation in captured)
                {
                    byte[] expected = Read(observation.Resource);
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
        await completed.Task.WaitAsync(BeamRaiseTraceHostNames.Deadline);
        Require((await actor.Completion.WaitAsync(BeamRaiseTraceHostNames.Deadline))
            .Equals(Term.A(BeamRaiseTraceHostNames.Normal)));
    }

    private static byte[] Read(string name)
    {
        using Stream input = typeof(BeamRaiseTraceHostChecks).Assembly
            .GetManifestResourceStream(BeamRaiseTraceHostNames.ResourcePrefix + name)
            ?? throw new InvalidDataException(BeamRaiseTraceHostNames.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamRaiseTraceHostNames.Digests[name]);

        return bytes;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(BeamRaiseTraceHostNames.Failure);
    }
}
