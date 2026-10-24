using System.Text;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProcessDisplayHostChecks
{
    public static async Task Run()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(streams);
        string source = "-module(process_display_host). -export([run/1]). run(S)->outer_12(S),ok. inner(S)->S!ready,receive stop->ok end.";
        for (int depth = 1; depth <= ProcessDisplayHostParameters.ChainDepth; depth++)
            source += " outer_" + depth + "(S)->" + (depth == 1 ? "inner" : "outer_" + (depth - 1)) + "(S),ok.";
        using var artifact = GeneratedModuleCompiler.Compile(source);
        artifact.Definition.Register(runtime.Modules);
        Exception? failure = null;
        ProcessHandle controller = runtime.Spawn(async context =>
        {
            try
            {
                ProcessHandle target = runtime.Spawn(child => runtime.Modules.Call(
                    child,
                    ProcessDisplayHostNames.Module,
                    "run",
                    context.Self
                ));
                Require((await context.ReceiveAsync(message => message, ProcessDisplayHostParameters.Timeout))!.Equals(Term.A(ProcessDisplayHostNames.Ready)));
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.A(ProcessDisplayHostNames.Label)
                );
                long before = error.Length;
                Term result = await Display(context, target.Pid);
                Require(result.Equals(Term.A(ErlangBooleanAtoms.True)));
                string text = Encoding.UTF8.GetString(error.ToArray())[(int)before..];
                Require(text.Contains(target.Pid.ToString(), StringComparison.Ordinal));
                Require(text.Contains(ProcessDisplayHostNames.CurrentInner, StringComparison.Ordinal));
                Require(text.Contains(ProcessDisplayHostNames.CallerRun, StringComparison.Ordinal));
                for (int depth = 1; depth <= ProcessDisplayHostParameters.ChainDepth; depth++)
                    Require(text.Contains("Called from: process_display_host:outer_" + depth + "/1\n", StringComparison.Ordinal));
                Require(!text.Contains(ProcessDisplayHostNames.Caller, StringComparison.Ordinal));
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.Info,
                    Term.A(SequenceTraceNames.Label)
                )).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), Term.A(ProcessDisplayHostNames.Label))));
                runtime.Send(target.Pid, Term.A(ProcessDisplayHostNames.Stop));
                await target.Completion.WaitAsync(ProcessDisplayHostParameters.Timeout);

                for (int round = 0; round < ProcessDisplayHostParameters.DeathRounds; round++)
                {
                    var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    ProcessHandle held = runtime.Spawn(async child =>
                    {
                        using var frame = child.EnterFunction(ProcessDisplayHostNames.Module, ProcessDisplayHostNames.Held, 0);
                        entered.SetResult();
                        await release.Task.WaitAsync(child.Cancellation);
                        await child.ReceiveAsync(message => message);

                        return Nil.Value;
                    });
                    await entered.Task.WaitAsync(ProcessDisplayHostParameters.Timeout);
                    long start = error.Length;
                    ValueTask<Term> pending = Display(context, held.Pid);
                    Require(!pending.IsCompleted);
                    if (round % 2 == 0)
                    {
                        runtime.Exit(context, held.Pid, Term.A(ProcessExitSignals.Kill));
                        try
                        {
                            await pending.AsTask().WaitAsync(ProcessDisplayHostParameters.Timeout);
                            Require(false);
                        }
                        catch (ErlangException exception)
                        {
                            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                        }
                        Require(error.Length == start);
                    }
                    else
                    {
                        release.SetResult();
                        Require((await pending.AsTask().WaitAsync(ProcessDisplayHostParameters.Timeout)).Equals(Term.A(ErlangBooleanAtoms.True)));
                        Require(error.Length > start);
                        runtime.Send(held.Pid, Term.A(ProcessDisplayHostNames.Stop));
                    }
                    await held.Completion.WaitAsync(ProcessDisplayHostParameters.Timeout);
                }

                var ownerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var ownerRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var requesterQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                ProcessHandle owner = runtime.Spawn(async child =>
                {
                    using var frame = child.EnterFunction(ProcessDisplayHostNames.Module, ProcessDisplayHostNames.Held, 0);
                    ownerEntered.SetResult();
                    await ownerRelease.Task.WaitAsync(child.Cancellation);
                    await child.ReceiveAsync(message => message);

                    return Nil.Value;
                });
                await ownerEntered.Task.WaitAsync(ProcessDisplayHostParameters.Timeout);
                long cancellationStart = error.Length;
                ProcessHandle requester = runtime.Spawn(async caller =>
                {
                    using var frame = caller.EnterFunction(ProcessDisplayHostNames.Module, ProcessDisplayHostNames.Caller, 0);
                    ValueTask<Term> pending = Display(caller, owner.Pid);
                    Require(!pending.IsCompleted);
                    requesterQueued.SetResult();
                    await pending;

                    return Nil.Value;
                });
                await requesterQueued.Task.WaitAsync(ProcessDisplayHostParameters.Timeout);
                runtime.Exit(context, requester.Pid, Term.A(ProcessExitSignals.Kill));
                await requester.Completion.WaitAsync(ProcessDisplayHostParameters.Timeout);
                ownerRelease.SetResult();
                ValueTask<Term> subsequent = Display(context, owner.Pid);
                Require((await subsequent.AsTask().WaitAsync(ProcessDisplayHostParameters.Timeout)).Equals(Term.A(ErlangBooleanAtoms.True)));
                string cancellationText = Encoding.UTF8.GetString(error.ToArray())[(int)cancellationStart..];
                Require(cancellationText.Split(owner.Pid.ToString(), StringSplitOptions.None).Length == 3);
                Require(!cancellationText.Contains(ProcessDisplayHostNames.Caller, StringComparison.Ordinal));
                runtime.Send(owner.Pid, Term.A(ProcessDisplayHostNames.Stop));
                await owner.Completion.WaitAsync(ProcessDisplayHostParameters.Timeout);
                Require(output.Length == 0);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        await controller.Completion.WaitAsync(ProcessDisplayHostParameters.Timeout);
        if (failure is not null)
            throw new InvalidOperationException(ProcessDisplayHostNames.Failure, failure);
    }

    private static ValueTask<Term> Display(ProcessContext context, Pid target) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ProcessDisplayNames.Function,
        target,
        Term.A(ProcessDisplayNames.Backtrace)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessDisplayHostNames.Failure);
    }
}
