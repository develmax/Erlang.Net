using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamLifecycleHostChecks
{
    internal static Task Deep() => Run(false);

    internal static Task Alternating() => Run(false, true);

    internal static Task Image() => Run(true);

    internal static PreparedBeamImage[] PublishDeep(ProcessRuntime runtime)
        => PublishPair(
            runtime,
            BeamLifecycleHostNames.Deep,
            BeamLifecycleHostNames.DeepSubject,
            BeamLifecycleHostNames.DeepObserver
        );

    private static PreparedBeamImage[] PublishPair(
        ProcessRuntime runtime,
        string group,
        string subject,
        string observer
    )
    {
        string[] modules = [subject, observer];
        PreparedBeamImage[] images = modules.Select(module => Prepare(
            runtime,
            group,
            module,
            [BeamLifecycleHostNames.Erlang, .. modules]
        )).ToArray();
        Require(runtime.Modules.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage image in images)
            CheckOwner(runtime, image);

        return images;
    }

    private static async Task Run(bool imageGenerations, bool alternating = false)
    {
        await using var runtime = new ProcessRuntime();
        var controls = new BeamImageGenerationControl();
        string group = imageGenerations ? BeamLifecycleHostNames.Image : alternating ? BeamLifecycleHostNames.Alternating : BeamLifecycleHostNames.Deep;
        string observer = imageGenerations ? BeamLifecycleHostNames.ImageObserver : alternating ? BeamLifecycleHostNames.AlternatingObserver : BeamLifecycleHostNames.DeepObserver;
        Term[] arguments = [];
        if (imageGenerations)
        {
            byte[][] generations = Enumerable.Range(1, BeamLifecycleHostNames.GenerationCount)
                .Select(index => Read(group + BeamLifecycleHostNames.GenerationPrefix + index + BeamLifecycleHostNames.BeamExtension)).ToArray();
            controls.Install(runtime, generations);
            arguments = generations.Select(bytes => (Term)new BitString(bytes)).ToArray();
            PreparedBeamImage image = Prepare(
                runtime,
                group,
                observer,
                [BeamLifecycleHostNames.Erlang, BeamImageGenerationNames.Module, BeamImageGenerationNames.Controller]
            );
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            CheckOwner(runtime, image);
        }
        else if (alternating)
            PublishPair(
                runtime,
                group,
                BeamLifecycleHostNames.AlternatingSubject,
                observer
            );
        else
            PublishDeep(runtime);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessContext? owner = null;
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            owner = context;
            try
            {
                var observed = new List<(Term Actual, byte[] Encoded, string Resource)>();
                foreach ((string entry, string resource) in new[]
                {
                    (BeamLifecycleHostNames.Run, BeamLifecycleHostNames.Values),
                    (BeamLifecycleHostNames.StackEntry, BeamLifecycleHostNames.Stacks)
                })
                {
                    Term actual = await runtime.Modules.Call(
                        context,
                        observer,
                        entry,
                        arguments
                    );
                    int cases = imageGenerations ? BeamLifecycleHostNames.ImageCases : alternating ? BeamLifecycleHostNames.AlternatingCases : BeamLifecycleHostNames.DeepCases;
                    if (entry == BeamLifecycleHostNames.Run)
                        Require(Cons.Items(actual).Count() == cases);
                    else
                    {
                        int profiles = imageGenerations ? BeamLifecycleHostNames.ImageProfiles : BeamLifecycleHostNames.DeepProfiles;
                        int rowsIndex = imageGenerations ? BeamLifecycleHostNames.ImageRowsIndex : BeamLifecycleHostNames.DeepRowsIndex;
                        Require(Cons.Items(actual).Count() == profiles);
                        Require(Cons.Items(actual).All(row => Cons.Items(((TupleTerm)row).Items[rowsIndex]).Count() == cases));
                    }
                    observed.Add((actual, ExternalTermFormat.Encode(
                        actual,
                        new EtfEncodingOptions { MinorVersion = BeamLifecycleHostNames.EtfMinorVersion, FunctionOwner = runtime.InitializationPid }
                    ), resource));
                }
                foreach (var observation in observed)
                {
                    byte[] original = Read(group + observation.Resource);
                    Require(observation.Encoded.AsSpan().SequenceEqual(original));
                    Require(observation.Actual.Equals(runtime.ImportExternalTerm(original).Term));
                }
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(BeamLifecycleHostNames.Deadline);
        Require((await actor.Completion.WaitAsync(BeamLifecycleHostNames.Deadline)).Equals(Term.A(BeamLifecycleHostNames.Normal)));
        if (imageGenerations)
            await controls.Settle(runtime, owner!, BeamLifecycleHostNames.Deadline);
        else
        {
            await BeamDeepFrameAccess.Unwound(owner!).WaitAsync(BeamLifecycleHostNames.Deadline);
            Require(BeamDeepFrameAccess.Active(
                owner!,
                runtime.Modules.CurrentCodeImage(alternating ? BeamLifecycleHostNames.AlternatingSubject : BeamLifecycleHostNames.DeepSubject)!
            ) == 0);
            Require(BeamDeepFrameAccess.Active(owner!, runtime.Modules.CurrentCodeImage(observer)!) == 0);
        }
    }

    private static PreparedBeamImage Prepare(
        ProcessRuntime runtime,
        string group,
        string module,
        string[] dependencies
    ) => runtime.Modules.PrepareBeam(Read(group + module + BeamLifecycleHostNames.BeamExtension), new BeamDependencySelection(dependencies));

    private static void CheckOwner(ProcessRuntime runtime, PreparedBeamImage image)
        => Require(runtime.Modules.Exports.Where(entry => entry.Module == image.Name)
            .All(entry => ReferenceEquals(image, runtime.Modules.SourceOwner(entry.Module, entry.Function, entry.Arity))));

    private static byte[] Read(string name)
    {
        using Stream stream = typeof(BeamLifecycleHostChecks).Assembly.GetManifestResourceStream(BeamLifecycleHostNames.Prefix + name)
            ?? throw new InvalidDataException(BeamLifecycleHostNames.Failure);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamLifecycleHostNames.Digests[name]);

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(BeamLifecycleHostNames.Failure);
    }
}
