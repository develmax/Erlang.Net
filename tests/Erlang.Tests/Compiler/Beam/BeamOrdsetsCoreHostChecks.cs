using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamOrdsetsCoreHostChecks
{
    internal static Task Run()
        => RunSubject(
            BeamOrdsetsCoreHostNames.Observer,
            Read,
            BeamOrdsetsCoreHostNames.ValueCount,
            BeamOrdsetsCoreHostNames.ProfileCount
        );

    internal static async Task RunSubject(
        string observer,
        Func<string, byte[]> read,
        int valueCount,
        int profileCount,
        Func<ProcessRuntime, Task>? initialize = null,
        bool encodeInRuntime = false,
        string[]? additionalDependencies = null
    )
    {
        byte[] lists = Read(BeamOrdsetsCoreHostNames.Lists + BeamOrdsetsCoreHostNames.BeamExtension);
        byte[] ordsets = Read(BeamOrdsetsCoreHostNames.Ordsets + BeamOrdsetsCoreHostNames.BeamExtension);
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(BeamOrdsetsCoreHostNames.Lists, lists),
            RuntimeModuleSelection.Beam(BeamOrdsetsCoreHostNames.Ordsets, ordsets, [BeamOrdsetsCoreHostNames.Lists])
        ]));
        if (initialize is not null)
            await initialize(runtime);
        VerifyLibrary(runtime, BeamOrdsetsCoreHostNames.Ordsets);
        VerifyLibrary(runtime, BeamOrdsetsCoreHostNames.Lists);
        string[] dependencies = [BeamOrdsetsCoreHostNames.Erlang, BeamOrdsetsCoreHostNames.Lists,
            BeamOrdsetsCoreHostNames.Ordsets, .. BeamOrdsetsCoreHostNames.Fixtures];
        string[] fixtures = [observer, BeamOrdsetsCoreHostNames.Support, BeamOrdsetsCoreHostNames.Worker];
        dependencies = [.. dependencies, BeamOrdsetsCoreHostNames.Binary, observer, .. additionalDependencies ?? []];
        PreparedBeamImage[] images = fixtures.Select(module => runtime.Modules.PrepareBeam(
            read(module + BeamOrdsetsCoreHostNames.BeamExtension), new BeamDependencySelection(dependencies)
        )).ToArray();
        Require(runtime.Modules.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage image in images)
            Require(runtime.Modules.Exports.Where(value => value.Module == image.Name)
                .All(value => ReferenceEquals(image, runtime.Modules.SourceOwner(value.Module, value.Function, value.Arity))));
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                var captured = new List<(Term Actual, byte[] Encoded, string Resource)>();
                foreach (var observation in BeamOrdsetsCoreHostNames.Observations)
                {
                    Term actual = await runtime.Modules.Call(
                        context,
                        BeamOrdsetsCoreHostNames.Worker,
                        BeamOrdsetsCoreHostNames.Run,
                        Term.A(observer),
                        Term.A(observation.Entry)
                    );
                    byte[] encoded = encodeInRuntime
                        ? ((BitString)await runtime.Modules.Call(
                            context,
                            BeamOrdsetsCoreHostNames.Erlang,
                            BeamOrdsetsCoreHostNames.TermToBinary,
                            actual
                        )).ToArray()
                        : ExternalTermFormat.Encode(actual, new EtfEncodingOptions { FunctionOwner = runtime.InitializationPid });
                    captured.Add((actual, encoded, observation.Resource));
                    int count = observation.Entry == BeamOrdsetsCoreHostNames.Run
                        ? Cons.Items(actual).Count()
                        : Cons.Items(actual).Sum(value => Cons.Items(((TupleTerm)value).Items[1]).Count());
                    Require(count == (observation.Entry == BeamOrdsetsCoreHostNames.Run ? valueCount : profileCount));
                }
                foreach (var observation in captured)
                {
                    byte[] expected = read(observation.Resource);
                    if (!observation.Encoded.AsSpan().SequenceEqual(expected))
                    {
                        string directory = Path.Combine(BeamOrdsetsCoreHostNames.FailureDirectory, observer);
                        Directory.CreateDirectory(directory);
                        foreach (var raw in captured)
                        {
                            File.WriteAllBytes(Path.Combine(directory, raw.Resource + BeamOrdsetsCoreHostNames.ActualSuffix), raw.Encoded);
                            File.WriteAllBytes(Path.Combine(directory, raw.Resource + BeamOrdsetsCoreHostNames.ExpectedSuffix), read(raw.Resource));
                        }
                        throw new InvalidDataException(BeamOrdsetsCoreHostNames.Failure + Environment.NewLine + Path.GetFullPath(directory));
                    }
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
        await completed.Task.WaitAsync(BeamOrdsetsCoreHostNames.Deadline);
        Require((await actor.Completion.WaitAsync(BeamOrdsetsCoreHostNames.Deadline)).Equals(Term.A(BeamOrdsetsCoreHostNames.Normal)));
    }

    internal static byte[] Read(string name)
    {
        using Stream input = typeof(BeamOrdsetsCoreHostChecks).Assembly.GetManifestResourceStream(BeamOrdsetsCoreHostNames.ResourcePrefix + name)
            ?? throw new InvalidDataException(BeamOrdsetsCoreHostNames.Failure);
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        byte[] result = bytes.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(result)) == BeamOrdsetsCoreHostNames.Digests[name]);

        return result;
    }

    private static void VerifyLibrary(ProcessRuntime runtime, string module)
    {
        var exports = runtime.Modules.Exports.Where(value => value.Module == module && value.Function != BeamOrdsetsCoreHostNames.ModuleInfo).ToArray();
        object? owner = exports.Length == 0 ? null : runtime.Modules.SourceOwner(module, exports[0].Function, exports[0].Arity);
        Require(owner is PreparedBeamImage && exports.All(value => ReferenceEquals(owner,
            runtime.Modules.SourceOwner(module, value.Function, value.Arity))));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(BeamOrdsetsCoreHostNames.Failure);
    }
}
