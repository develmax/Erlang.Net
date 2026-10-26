using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.LogicalRetirement502Names;

namespace Erlang.Tests;

internal static class LogicalRetirement502Checks
{
    internal static async Task Run()
    {
        byte[] manifest = Resource(Manifest);
        Check(Convert.ToHexString(SHA256.HashData(manifest)) == ManifestSha256);
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)!;
        foreach (var pair in hashes)
            Check(Convert.ToHexString(SHA256.HashData(Resource(pair.Key))) == pair.Value);

        Term actual = await Observe();
        Check(ExternalTermFormat.Encode(actual).AsSpan().SequenceEqual(Resource(Returned)));
    }

    private static async Task<Term> Observe()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapProcessAlive425.Register(runtime, registry));
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var queryActor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext child = await target.Ready;
        ReferenceTerm monitor = await observer.Invoke(context => runtime.Monitor(context, child.Self));
        using var enteredEvent = new ManualResetEventSlim();
        using var releaseEvent = new ManualResetEventSlim();
        int entered = 0;
        int done = 0;
        int waitResult = WaitPending;

        Term Status() => Term.Tuple(
            Term.I(Volatile.Read(ref entered)),
            Term.I(Volatile.Read(ref done)),
            Term.I(Volatile.Read(ref waitResult))
        );

        Task<bool> work = target.InvokeAsync(async context =>
        {
            runtime.Register(Registered, context.Self);
            await DirtyNative419Access.Schedule(
                context,
                DirtyNative419Names.Cpu,
                [],
                (_, _) =>
                {
                    Volatile.Write(ref entered, 1);
                    enteredEvent.Set();
                    bool released = releaseEvent.Wait(Deadline);
                    Volatile.Write(ref waitResult, released ? WaitReleased : WaitTimedOut);
                    Volatile.Write(ref done, 1);

                    return released ? ReleasedAtom : TimeoutAtom;
                }
            );

            return true;
        });
        Task<Term>? publicQuery = null;
        try
        {
            Check(enteredEvent.Wait(Deadline));
            Check(DirtyNative419Access.Field<int>(DirtyNative419Access.Snapshot(runtime), DirtyNative419Names.Running) == 1);
            Term beforeEntry = Status();
            await observer.Invoke(context =>
            {
                runtime.Exit(context, child.Self, KillAtom);

                return true;
            });
            var queryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            publicQuery = queryActor.InvokeAsync(async context =>
            {
                queryStarted.TrySetResult();

                return await runtime.Modules.Call(
                    context,
                    ProcessLiveness499Names.Erlang,
                    ProcessLiveness499Names.Alive,
                    child.Self
                );
            });
            await queryStarted.Task.WaitAsync(Deadline);
            Term alive = await publicQuery.WaitAsync(Deadline);
            Term native = await observer.InvokeAsync(async context =>
            {
                ReferenceTerm reference = runtime.MakeReference();
                await runtime.Modules.Call(
                    context,
                    Internal,
                    ProcessLiveness499Names.Alive,
                    child.Self,
                    reference
                );
                TupleTerm? reply = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(reference) ? tuple : null,
                    Deadline,
                    context.Cancellation
                );
                Check(reply is not null);

                return reply!.Items[1];
            });
            Term name = runtime.WhereIs(Registered);
            Term down = await observer.InvokeAsync(async context =>
            {
                TupleTerm? message = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(DownAtom) &&
                        tuple.Items[1].Equals(monitor) && tuple.Items[3].Equals(child.Self) ? tuple : null,
                    Deadline,
                    context.Cancellation
                );
                Check(message is not null);

                return message!.Items[4];
            });
            Term beforeRelease = Status();
            Check(alive.Equals(FalseAtom) && native.Equals(FalseAtom) && down.Equals(KilledAtom));
            Check(Volatile.Read(ref done) == 0 && Volatile.Read(ref waitResult) == WaitPending);
            Check(!target.Process.Completion.IsCompleted);
            Term observed = Term.Tuple(
                BeforeAtom,
                beforeEntry,
                queryStarted.Task.IsCompletedSuccessfully ? TrueAtom : FalseAtom,
                alive,
                native,
                name,
                down,
                beforeRelease
            );
            releaseEvent.Set();
            Check((await target.Process.Completion.WaitAsync(Deadline)).Equals(KilledAtom));
            await JoinWork(work);
            Check(Volatile.Read(ref done) == 1 && Volatile.Read(ref waitResult) == WaitReleased);

            return Term.Tuple(observed, Term.Tuple(AfterAtom, Status()));
        }
        finally
        {
            // The host completion joins physical work; release before any actor/runtime disposal.
            releaseEvent.Set();
            await JoinWork(work);
            if (publicQuery is not null)
                await publicQuery.WaitAsync(Deadline);
        }
    }

    private static async Task JoinWork(Task<bool> work)
    {
        try
        {
            await work.WaitAsync(Deadline);
        }
        catch (OperationCanceledException) { }
        catch (ErlangException failure) when (failure.Reason.Equals(KilledAtom)) { }
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(LogicalRetirement502Checks).Assembly.GetManifestResourceStream(Prefix + name)
            ?? throw new InvalidDataException(Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(Failure);
    }
}
