using System.Reflection;
using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamAwaitEntryHostChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static Task Remote() => Run(
        BeamAwaitEntryHostNames.Remote,
        BeamAwaitEntryHostNames.RemoteObserver,
        [BeamAwaitEntryHostNames.RemotePeer, BeamAwaitEntryHostNames.RemoteCaller, BeamAwaitEntryHostNames.RemoteObserver],
        BeamAwaitEntryHostNames.RemoteCases,
        false
    );

    internal static Task Native() => Run(
        BeamAwaitEntryHostNames.Native,
        BeamAwaitEntryHostNames.NativeObserver,
        [BeamAwaitEntryHostNames.Lists, BeamAwaitEntryHostNames.NativeObserver],
        BeamAwaitEntryHostNames.NativeCases,
        true
    );

    internal static Task Delay() => Run(
        BeamDelayProtocolNames.Group,
        BeamDelayProtocolNames.Module,
        [BeamDelayProtocolNames.Module],
        BeamDelayProtocolNames.Cases,
        false
    );

    private static async Task Run(
        string group,
        string observer,
        string[] modules,
        int cases,
        bool native
    )
    {
        await using var runtime = new ProcessRuntime();
        string[] dependencies = [BeamAwaitEntryHostNames.Erlang, .. modules];
        PreparedBeamImage[] images = modules.Select(module => runtime.Modules.PrepareBeam(
            Read(group + module + BeamAwaitEntryHostNames.BeamExtension),
            new BeamDependencySelection(dependencies)
        )).ToArray();
        if (native)
            Require(images[0].NativePrimitiveExports.Select(signature => signature.Name + BeamAwaitEntryHostNames.AritySeparator + signature.Arity)
                .Order().SequenceEqual(BeamAwaitEntryHostNames.PrimitiveSignatures));
        Require(runtime.Modules.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage image in images)
            Require(runtime.Modules.Exports.Where(entry => entry.Module == image.Name)
                .All(entry => ReferenceEquals(image, runtime.Modules.SourceOwner(entry.Module, entry.Function, entry.Arity))));
        ModuleCodeImage[] selected = modules.Select(module => runtime.Modules.CurrentCodeImage(module)!).ToArray();
        ProcessContext? owner = null;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            owner = context;
            try
            {
                var observations = new List<(Term Actual, byte[] Encoded, string Resource)>();
                foreach ((string entry, string resource) in new[]
                {
                    (BeamAwaitEntryHostNames.Run, BeamAwaitEntryHostNames.Values),
                    (BeamAwaitEntryHostNames.Stacks, BeamAwaitEntryHostNames.StackFile)
                })
                {
                    Term actual = await runtime.Modules.Call(
                        context,
                        observer,
                        entry,
                        []
                    );
                    Require(entry == BeamAwaitEntryHostNames.Run
                        ? Cons.Items(actual).Count() == cases
                        : Cons.Items(actual).Count() == BeamAwaitEntryHostNames.Profiles && Cons.Items(actual).All(row => Cons.Items(((TupleTerm)row).Items[1]).Count() == cases));
                    observations.Add((actual, ExternalTermFormat.Encode(
                        actual,
                        new EtfEncodingOptions { MinorVersion = BeamAwaitEntryHostNames.EtfMinorVersion, FunctionOwner = runtime.InitializationPid }
                    ), resource));
                }
                foreach (var observation in observations)
                {
                    byte[] original = Read(group + observation.Resource);
                    Require(observation.Encoded.AsSpan().SequenceEqual(original));
                    Require(observation.Actual.Equals(runtime.ImportExternalTerm(original).Term));
                }
                completion.TrySetResult();
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }

            return Nil.Value;
        });
        await completion.Task.WaitAsync(BeamAwaitEntryHostNames.Deadline);
        Require((await actor.Completion.WaitAsync(BeamAwaitEntryHostNames.Deadline))
            .Equals(Term.A(BeamAwaitEntryHostNames.Normal)));
        var unwound = (TaskCompletionSource)typeof(ProcessContext).GetProperty(BeamAwaitEntryHostNames.Unwound, PrivateInstance)!.GetValue(owner)!;
        await unwound.Task.WaitAsync(BeamAwaitEntryHostNames.Deadline);
        object gate = typeof(ProcessRuntime).GetField(BeamAwaitEntryHostNames.Gate, PrivateInstance)!.GetValue(runtime)!;
        ProcessContext[] contexts;
        lock (gate)
        {
            var executing = (HashSet<ProcessContext>)typeof(ProcessRuntime).GetField(BeamAwaitEntryHostNames.Executing, PrivateInstance)!.GetValue(runtime)!;
            contexts = executing.Append(owner!).Distinct().ToArray();
        }
        MethodInfo isExecuting = typeof(ProcessContext).GetMethod(BeamAwaitEntryHostNames.IsExecutingCode, PrivateInstance)!;
        foreach (ModuleCodeImage image in selected)
            foreach (ProcessContext context in contexts)
                Require(!(bool)isExecuting.Invoke(context, [image])!);
    }

    private static byte[] Read(string name)
    {
        using Stream input = typeof(BeamAwaitEntryHostChecks).Assembly.GetManifestResourceStream(BeamAwaitEntryHostNames.Prefix + name)
            ?? throw new InvalidDataException(BeamAwaitEntryHostNames.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamAwaitEntryHostNames.Digests[name]);

        return bytes;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(BeamAwaitEntryHostNames.Failure);
    }
}
