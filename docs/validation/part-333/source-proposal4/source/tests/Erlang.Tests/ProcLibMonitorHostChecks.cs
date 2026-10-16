using System.Reflection;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibMonitorHostChecks
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
        Term tailMarker = Term.A(ProcLibMonitorHostNames.TailMarker);
        Term ok = Term.A(ProcLibStartupWaitHostNames.Ok);
        Exception? failure = null;
        var parent = runtime.Spawn(async context =>
        {
            try
            {
                runtime.Send(context.Self, unrelated);
                var child = runtime.SpawnMonitor(
                    context,
                    async actor =>
                {
                    if (!deadBeforeWait)
                    {
                        runtime.Send(context.Self, Term.Tuple(Term.A(tag), actor.Self, reply));
                        queued.TrySetResult();
                        await release.Task;
                    }

                    return ok;
                }
                );
                if (deadBeforeWait)
                    await child.Process.Completion;
                else
                    await queued.Task;
                runtime.Send(context.Self, tailMarker);
                var method = typeof(OtpModules).Assembly.GetType(ProcLibStartupWaitHostNames.Type, true)!.GetMethod(ProcLibMonitorHostNames.Method, BindingFlags.Static | BindingFlags.NonPublic)!;
                var operation = ((ValueTask<Term>)method.Invoke(null, [context, child.Process.Pid, child.Reference, Term.A(ProcLibStartupWaitHostNames.InvalidTimeout)])!).AsTask();
                if (tag == ProcLibStartupWaitHostNames.Nack)
                {
                    if (operation.IsCompleted)
                    {
                        await operation;
                        throw new InvalidOperationException(ProcLibMonitorHostNames.Failure);
                    }
                    release.TrySetResult();
                }
                Term result = await operation;
                Term expectedReturn = deadBeforeWait ? Term.Tuple(Term.A(ProcLibStartupWaitHostNames.Error), Term.A(ProcessExitReasons.Normal)) : reply;
                if (!result.Equals(Term.Tuple(expectedReturn, child.Reference)) || runtime.IsAlive(child.Process.Pid) != (tag == ProcLibStartupWaitHostNames.Ack))
                    throw new InvalidOperationException(ProcLibMonitorHostNames.Failure);
                Term monitors = await runtime.Modules.Call(
                    context,
                    ProcLibStartupWaitHostNames.Erlang,
                    ProcLibStartupWaitHostNames.ProcessInfo,
                    context.Self,
                    Term.A(ProcLibStartupWaitHostNames.Monitors)
                );
                Term expectedMonitors = tag == ProcLibStartupWaitHostNames.Ack
                    ? Term.List(Term.Tuple(Term.A(ProcessMonitorKinds.Process), child.Process.Pid))
                    : Nil.Value;
                if (!monitors.Equals(Term.Tuple(Term.A(ProcLibStartupWaitHostNames.Monitors), expectedMonitors)))
                    throw new InvalidOperationException(ProcLibMonitorHostNames.Failure);
                Term? first = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                if (first is null || !first.Equals(unrelated))
                    throw new InvalidOperationException(ProcLibMonitorHostNames.Failure);
                Term? second = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                if (second is null || !second.Equals(tailMarker))
                    throw new InvalidOperationException(ProcLibMonitorHostNames.Failure);
                release.TrySetResult();
                await child.Process.Completion;
                Term? down = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                Term expectedDown = Term.Tuple(
                    Term.A(ProcLibStartupWaitHostNames.Down),
                    child.Reference,
                    Term.A(ProcessMonitorKinds.Process),
                    child.Process.Pid,
                    Term.A(ProcessExitReasons.Normal)
                );
                if (down is null || !down.Equals(expectedDown) || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                    throw new InvalidOperationException(ProcLibMonitorHostNames.Failure);

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
            throw new InvalidOperationException(ProcLibMonitorHostNames.Failure, failure);
    }
}
