using System.Reflection;
using System.Security.Cryptography;
using static Erlang.Tests.ProcessOwnedWord504Names;

namespace Erlang.Tests;

internal static class BeamOwnedWord504Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Replay()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = Load(runtime);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                foreach (var observation in new[]
                {
                    (Entry: BeamProtectedReceiveHostNames.Run, Resource: BeamProtectedReceiveHostNames.Values),
                    (Entry: BeamProtectedReceiveHostNames.RunStacks, Resource: BeamProtectedReceiveHostNames.Stacks)
                })
                {
                    Term result = await runtime.Modules.Call(
                        context,
                        image.Name,
                        observation.Entry,
                        []
                    );
                    byte[] encoded = ExternalTermFormat.Encode(result,
                        new EtfEncodingOptions { FunctionOwner = runtime.InitializationPid });
                    Check(encoded.AsSpan().SequenceEqual(Read(observation.Resource)));
                    RequireEmpty(context);
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
        Check((await actor.Completion.WaitAsync(Deadline)).Equals(Term.A(BeamProtectedReceiveHostNames.Normal)));
    }

    internal static async Task Held()
    {
        await Held(false);
        await Held(true);
    }

    private static async Task Held(bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = Load(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                Task<Term> pending = runtime.Modules.Call(
                    context,
                    image.Name,
                    BeamOwnedWord504Names.Held,
                    [runtime.InitializationPid]
                ).AsTask();
                Check(!pending.IsCompleted);
                using var admission = new CancellationTokenSource(Deadline);
                while (true)
                {
                    Check(!pending.IsCompleted);
                    object snapshot = Get(Get(context, BeamOwnedWord504Names.ContextArena), Snapshot);
                    bool waiting = (bool)typeof(ProcessContext).GetField(BeamOwnedWord504Names.Waiting, Members)!.GetValue(context)!;
                    if (waiting && (long)Get(snapshot, ReservedWords) > 0 && (long)Get(snapshot, RootedWords) > 0)
                        break;
                    await Task.Delay(1, admission.Token);
                }
                entered.TrySetResult(context);
                try
                {
                    Term result = await pending;
                    Check(!cancel && result.Equals(Term.A(BeamOwnedWord504Names.Released)));
                }
                catch (OperationCanceledException) when (cancel) { }
                RequireEmpty(context);
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                entered.TrySetException(error);
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        ProcessContext live = await entered.Task.WaitAsync(Deadline);
        Check((int)Get(Get(Get(live, BeamOwnedWord504Names.ContextArena), Snapshot), ActiveLeases) > 0);
        if (cancel)
            await killer.Invoke(context =>
            {
                runtime.Exit(context, actor.Pid, Term.A(BeamOwnedWord504Names.Kill));

                return true;
            });
        else
            runtime.Send(actor.Pid, Term.A(BeamOwnedWord504Names.Release));
        // Logical DOWN can precede the interpreter finally; join its actual body.
        await completed.Task.WaitAsync(Deadline);
        await actor.Completion.WaitAsync(Deadline);
        RequireEmpty(live);
    }

    private static PreparedBeamImage Load(ProcessRuntime runtime)
    {
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Read(BeamProtectedReceiveHostNames.Beam),
            new BeamDependencySelection([BeamProtectedReceiveHostNames.Erlang]));
        Check(runtime.Modules.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);

        return image;
    }

    private static byte[] Read(string name)
    {
        using Stream input = typeof(BeamOwnedWord504Checks).Assembly
            .GetManifestResourceStream(BeamProtectedReceiveHostNames.ResourcePrefix + name)
            ?? throw new InvalidDataException(Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        Check(Convert.ToHexString(SHA256.HashData(bytes)) == BeamProtectedReceiveHostNames.Digests[name]);

        return bytes;
    }

    private static void RequireEmpty(ProcessContext context)
    {
        object snapshot = Get(Get(context, BeamOwnedWord504Names.ContextArena), Snapshot);
        Check((long)Get(snapshot, ReservedWords) == 0);
        Check((long)Get(snapshot, RootedWords) == 0);
        Check((int)Get(snapshot, ActiveLeases) == 0);
    }

    private static object Get(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
