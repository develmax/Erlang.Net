using System.Reflection;
using Erlang.Compiler;
using Erlang.Differential;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibStartupWaitHostChecks
{
    public static async Task Run()
    {
        await Scenario(ProcLibStartupWaitHostNames.Ack, false);
        await Scenario(ProcLibStartupWaitHostNames.Nack, false);
        await Scenario(ProcLibStartupWaitHostNames.Down, true);
    }

    private static async Task Scenario(string tag, bool deadBeforeWait)
    {
        await using var runtime = new ProcessRuntime();
        OtpModules.Register(runtime.Modules);
        var queued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Term reply = Term.A(ProcLibStartupWaitHostNames.Reply);
        Term unrelated = Term.A(ProcLibStartupWaitHostNames.Unrelated);
        Term ok = Term.A(ProcLibStartupWaitHostNames.Ok);
        Exception? failure = null;
        var parent = runtime.Spawn(async context =>
        {
            try
            {
                runtime.Send(context.Self, unrelated);
                var child = runtime.SpawnMonitor(context, async actor =>
                {
                    if (!deadBeforeWait)
                    {
                        runtime.Send(context.Self, Term.Tuple(Term.A(tag), actor.Self, reply));
                        queued.TrySetResult();
                        await release.Task;
                    }

                    return ok;
                });
                if (deadBeforeWait)
                    await child.Process.Completion;
                else
                    await queued.Task;
                var method = typeof(OtpModules).Assembly.GetType(ProcLibStartupWaitHostNames.Type, true)!.GetMethod(ProcLibStartupWaitHostNames.Method, BindingFlags.Static | BindingFlags.NonPublic)!;
                var operation = ((ValueTask<Term>)method.Invoke(null, [context, child.Process.Pid, child.Reference, Term.A(ProcLibStartupWaitHostNames.InvalidTimeout)])!).AsTask();
                if (tag == ProcLibStartupWaitHostNames.Nack)
                {
                    if (operation.IsCompleted)
                    {
                        await operation;
                        throw new InvalidOperationException(ProcLibStartupWaitHostNames.Failure);
                    }
                    release.TrySetResult();
                }
                Term result = await operation;
                Term expected = deadBeforeWait ? Term.Tuple(Term.A(ProcLibStartupWaitHostNames.Error), Term.A(ProcessExitReasons.Normal)) : reply;
                if (!result.Equals(expected) || runtime.IsAlive(child.Process.Pid) != (tag == ProcLibStartupWaitHostNames.Ack))
                    throw new InvalidOperationException(ProcLibStartupWaitHostNames.Failure);
                Term monitors = await runtime.Modules.Call(
                    context,
                    ProcLibStartupWaitHostNames.Erlang,
                    ProcLibStartupWaitHostNames.ProcessInfo,
                    context.Self,
                    Term.A(ProcLibStartupWaitHostNames.Monitors)
                );
                if (!monitors.Equals(Term.Tuple(Term.A(ProcLibStartupWaitHostNames.Monitors), Nil.Value)))
                    throw new InvalidOperationException(ProcLibStartupWaitHostNames.Failure);
                Term? retained = await context.ReceiveAsync(value => value.Equals(unrelated) ? value : null, TimeSpan.Zero);
                if (retained is null || !retained.Equals(unrelated))
                    throw new InvalidOperationException(ProcLibStartupWaitHostNames.Failure);
                release.TrySetResult();
                await child.Process.Completion;

                return ok;
            }
            catch (Exception caught)
            {
                failure = caught;
                throw;
            }
            finally
            {
                release.TrySetResult();
            }
        });
        Term reason = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        if (failure is not null || !reason.Equals(Term.A(ProcessExitReasons.Normal)))
            throw new InvalidOperationException(ProcLibStartupWaitHostNames.Failure, failure);
    }

    public static async Task RunInterpreted()
    {
        (string Source, Term Expected)[] cases = [
            (ProcLibStartupWaitHostNames.SimpleExpression, Term.A(ProcLibStartupWaitHostNames.Matched)),
            (ProcLibStartupWaitHostNames.ExpressionOnce, Term.Tuple(Term.A(ProcLibStartupWaitHostNames.Matched), Term.I(1)))
        ];
        foreach (var control in cases.Concat(ReceiveWaitCases.Interpreted))
        {
            await using var runtime = new ProcessRuntime();
            Expr expression = new Parser(control.Source).ParseExpression();
            Term? result = null;
            var process = runtime.Spawn(async context =>
            {
                result = await Execution.EvaluateAsync(expression, context);

                return Term.A(ProcLibStartupWaitHostNames.Ok);
            });
            Term reason = await process.Completion.WaitAsync(TimeSpan.FromSeconds(30));
            if (!reason.Equals(Term.A(ProcessExitReasons.Normal)) || result is null || !result.Equals(control.Expected))
                throw new InvalidOperationException(ProcLibStartupWaitHostNames.Failure);
        }
    }
}
