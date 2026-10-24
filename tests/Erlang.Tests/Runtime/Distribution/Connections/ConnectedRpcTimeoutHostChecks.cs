using Erlang.Otp;
using System.Reflection;

namespace Erlang.Tests;

public static class ConnectedRpcTimeoutHostChecks
{
    public static Task QueuedDownBeforeZeroWait() => QueuedDown(false);

    public static Task TimeoutLosesToAdmittedDown() => QueuedDown(true);

    private static async Task QueuedDown(bool afterTimeout)
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(OtpModules.Register);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var ready = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await actor.InvokeAsync(async context =>
            {
                ReferenceTerm result = runtime.MakeReference();
                Term value = Term.Tuple(Term.A(ConnectedRpcHostNames.UserReason), new BitString([0, 255]));
                var callback = new FunctionTerm(
                    0,
                    async (execution, _) =>
                {
                    var worker = (ProcessContext)execution;
                    ready.TrySetResult(worker.Self);
                    await release.Task.WaitAsync(worker.Cancellation);
                    worker.Exit(Term.Tuple(result, Term.A(ConnectedRpcHostNames.Return), value));

                    return Nil.Value;
                }
                );
                var reference = (ReferenceTerm)await runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ProcessSpawnRequestNames.Function,
                    callback,
                    Term.List(
                        Term.A(ProcessSpawnOptionNames.Monitor),
                        Term.Tuple(Term.A(ProcessSpawnRequestNames.Reply), Term.A(ProcessSpawnRequestNames.ErrorOnly))
                    )
                );
                Pid child = await ready.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
                runtime.Send(context.Self, Term.A(ConnectedSpawnHostNames.Unrelated));
                // Admit the real suppressed startup marker while the child is still alive; preserve ordinary mail.
                Require(await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);
                release.TrySetResult();
                using var deadline = new CancellationTokenSource(ConnectedSpawnHostNames.Deadline);
                while (runtime.IsAlive(child))
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    await Task.Delay(ConnectedSpawnHostNames.PollMilliseconds, deadline.Token);
                }
                if (afterTimeout)
                {
                    // Admit the genuine DOWN without selecting it before invoking the normal timeout race resolver.
                    Require(await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);
                }
                Type type = typeof(Erlang.Otp.KernelModules).Assembly.GetType(ConnectedRpcHostNames.ErpcType)!;
                MethodInfo method = type.GetMethod(
                    afterTimeout ? ConnectedRpcHostNames.AfterTimeout : ConnectedRpcHostNames.WaitResult,
                    BindingFlags.Static | BindingFlags.NonPublic
                )!;
                object? operation = method.Invoke(null, afterTimeout
                    ? [context, reference, result]
                    : [context, reference, result, Term.I(0)]);
                Term received = await (ValueTask<Term>)operation!;
                Require(received.Equals(value));
                Require((await context.ReceiveAsync(item => item, TimeSpan.Zero))!.Equals(Term.A(ConnectedSpawnHostNames.Unrelated)));
                Require(await context.ReceiveAsync(item => item, TimeSpan.Zero) is null);

                return true;
            });
        }
        finally
        {
            release.TrySetResult();
        }
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
