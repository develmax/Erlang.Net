using System.Reflection;
using Erlang;

namespace Erlang.Tests;

public static class BeamReceiveHostChecks
{
    public static async Task Run()
    {
        byte[] genuine = Resource(BeamReceiveHostNames.Resource);
        BeamModule module = BeamModule.Prepare(genuine);
        Require(module.Supports(BeamReceiveHostNames.Run, BeamReceiveHostNames.RunArity));
        await using var runtime = new ProcessRuntime();
        var senderReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSender = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle sender = runtime.Spawn(async context =>
        {
            senderReady.TrySetResult(context);

            return await releaseSender.Task;
        });
        ProcessContext controller = await senderReady.Task;
        try
        {
            Term unmatched = Term.A(BeamReceiveHostNames.Unrelated);
            Term expected = Term.Tuple(Term.A(BeamReceiveHostNames.Result), Term.A(BeamReceiveHostNames.Match));
            var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
            var leftover = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle owner = runtime.Spawn(async context =>
            {
                context.Mailbox.Send(unmatched);
                try
                {
                    Term actual = await module.Invoke(context, BeamReceiveHostNames.Run);
                    observed.TrySetResult(actual);
                    Require(context.Mailbox.Count == BeamReceiveHostNames.RetainedMessageCount);
                    leftover.TrySetResult((await context.ReceiveAsync(value => value, TimeSpan.Zero))!);

                    return actual;
                }
                catch (Exception error)
                {
                    observed.TrySetException(error);
                    leftover.TrySetException(error);
                    throw;
                }
            });
            Term nativeValue = await observed.Task;
            Require(nativeValue.Equals(expected) && (await leftover.Task).Equals(unmatched));
            Require(ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamReceiveHostNames.Result), nativeValue)).AsSpan().SequenceEqual(Resource(BeamReceiveHostNames.NativeExpected)));
            _ = await owner.Completion;
            BeamModule modeled = BeamModule.Prepare(BeamReceiveModeledBytes.WithoutSelfSend(genuine));
            await ControlledArrival(
                runtime,
                controller,
                modeled,
                expected,
                false
            );
            await ControlledArrival(
                runtime,
                controller,
                modeled,
                expected,
                true
            );
            await ControlledCancellation(runtime, controller, modeled);
        }
        finally
        {
            releaseSender.TrySetResult(Term.A(BeamReceiveHostNames.Result));
            _ = await sender.Completion;
        }
    }

    private static async Task ControlledArrival(
        ProcessRuntime runtime,
        ProcessContext controller,
        BeamModule modeled,
        Term expected,
        bool suspended
    )
    {
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await modeled.Invoke(context, BeamReceiveHostNames.Run);
                observed.TrySetResult(result);

                return result;
            }
            catch (Exception error)
            {
                observed.TrySetException(error);
                throw;
            }
        });
        await AwaitWaiting(controller, owner.Pid);
        Require(!observed.Task.IsCompleted);
        if (suspended)
            _ = await runtime.Modules.Call(
                controller,
                BeamReceiveHostNames.Erlang,
                BeamReceiveHostNames.Suspend,
                owner.Pid
            );
        ErlangOperatorValues.Send(controller, owner.Pid, Term.A(BeamReceiveHostNames.Match));
        if (suspended)
        {
            Require(!observed.Task.IsCompleted);
            _ = await runtime.Modules.Call(
                controller,
                BeamReceiveHostNames.Erlang,
                BeamReceiveHostNames.Resume,
                owner.Pid
            );
        }
        Require((await observed.Task).Equals(expected));
        _ = await owner.Completion;
    }

    private static async Task ControlledCancellation(ProcessRuntime runtime, ProcessContext controller, BeamModule modeled)
    {
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await modeled.Invoke(context, BeamReceiveHostNames.Run);
                observed.TrySetResult(result);

                return result;
            }
            catch (Exception error)
            {
                observed.TrySetException(error);
                throw;
            }
        });
        await AwaitWaiting(controller, owner.Pid);
        runtime.Exit(controller, owner.Pid, Term.A(BeamReceiveHostNames.Kill));
        Require((await owner.Completion).Equals(Term.A(BeamReceiveHostNames.Killed)));
        try
        {
            _ = await observed.Task;
            throw new InvalidOperationException(BeamReceiveHostNames.MissingCancellation);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task AwaitWaiting(ProcessContext controller, Pid target)
    {
        while (true)
        {
            Term info = await controller.Runtime.Modules.Call(
                controller,
                BeamReceiveHostNames.Erlang,
                BeamReceiveHostNames.Info,
                target,
                Term.A(BeamReceiveHostNames.Status)
            );
            if (info is TupleTerm tuple && tuple.Items[1].Equals(Term.A(BeamReceiveHostNames.Waiting)))
                return;
            await controller.ReduceAsync();
        }
    }

    private static byte[] Resource(string file)
    {
        Assembly assembly = typeof(BeamReceiveHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamReceiveHostNames.ResourceSeparator + file, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamReceiveHostNames.Failed);
    }
}
