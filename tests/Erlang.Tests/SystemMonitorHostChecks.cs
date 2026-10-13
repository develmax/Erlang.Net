namespace Erlang.Tests;

public static class SystemMonitorHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                foreach (string effect in new[] { SystemMonitorNames.LongGarbageCollection, SystemMonitorNames.LongSchedule, SystemMonitorNames.LargeHeap, SystemMonitorNames.BusyPort, SystemMonitorNames.BusyDistributionPort })
                {
                    Term enabled = effect is SystemMonitorNames.BusyPort or SystemMonitorNames.BusyDistributionPort ? Term.A(ErlangBooleanAtoms.True) : Term.I(0);
                    try
                    {
                        await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            SystemMonitorNames.Function,
                            context.Self,
                            Term.List(Term.Tuple(Term.A(effect), enabled))
                        );
                        throw new InvalidOperationException(SystemMonitorHostNames.Failure);
                    }
                    catch (ErlangException exception)
                    {
                        Require(exception.Reason.Equals(SystemMonitorNames.UnsupportedEffect(effect).Reason));
                    }
                    Require((await runtime.Modules.Call(context, ErlangModuleNames.Module, SystemMonitorNames.Function)).Equals(Term.A(SystemMonitorNames.Undefined)));
                }
                Term configuration = Term.List(Term.Tuple(
                    Term.A(SystemMonitorNames.LongQueue),
                    Term.Tuple(Term.I(SystemMonitorHostNames.DisableThreshold), Term.I(SystemMonitorHostNames.QueueMessages))
                ));
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SystemMonitorNames.Function,
                    context.Self,
                    configuration
                )).Equals(Term.A(SystemMonitorNames.Undefined)));
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var targets = Enumerable.Range(0, SystemMonitorHostNames.ConcurrentQueues).Select(_ => runtime.Spawn(async child =>
                {
                    await release.Task.WaitAsync(child.Cancellation);
                    for (int index = 0; index < SystemMonitorHostNames.QueueMessages; index++)
                    {
                        Term? item = await child.ReceiveAsync<Term>(term => term, SystemMonitorHostNames.Timeout);
                        Require(item is not null);
                    }

                    return Nil.Value;
                })).ToArray();
                await Task.WhenAll(targets.Select(target => Task.Run(() =>
                {
                    for (int index = 0; index < SystemMonitorHostNames.QueueMessages; index++)
                        runtime.Send(target.Pid, index == 0 ? Nil.Value : Term.I(index));
                })));
                release.SetResult();
                await Task.WhenAll(targets.Select(target => target.Completion));
                var events = targets.ToDictionary(target => target.Pid, _ => new List<bool>());
                for (int index = 0; index < SystemMonitorHostNames.ConcurrentQueues * 2; index++)
                {
                    Term? message = await context.ReceiveAsync<Term>(
                        term => term is TupleTerm { Items: [Atom { Name: SystemMonitorNames.Notification }, Pid, Atom { Name: SystemMonitorNames.LongQueue }, Atom] } ? term : null,
                        SystemMonitorHostNames.Timeout
                    );
                    Require(message is TupleTerm);
                    var tuple = (TupleTerm)message!;
                    events[(Pid)tuple.Items[1]].Add(tuple.Items[3].Equals(Term.A(ErlangBooleanAtoms.True)));
                }
                Require(events.Values.All(transitions => transitions.SequenceEqual(new[] { true, false })));
                Require(await context.ReceiveAsync<Term>(term => term, TimeSpan.Zero) is null);
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SystemMonitorNames.Function,
                    Term.A(SystemMonitorNames.Undefined)
                );
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(SystemMonitorHostNames.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SystemMonitorHostNames.Failure);
    }
}
