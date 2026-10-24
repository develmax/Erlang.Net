using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsHeir419MonitorChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void Require(bool v)
    {
        if (!v) throw new InvalidOperationException(EtsHeir419HostNames.Failure);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime r,
        ProcessContext c,
        string f,
        params Term[] a
    ) => r.Modules.Call(
        c,
        EtsHeir419HostNames.Ets,
        f,
        a
    );

    internal static async Task Run()
    {
        foreach (bool terminate in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
            RuntimeBootstrapEtsHeir419.Register(runtime, runtime.Modules);
            await using var giver = new RuntimeNodeFoundationHostActor(runtime);
            await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
            await using var observer = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext target = await receiver.Ready;
            _ = await observer.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                ErlangModuleNames.Module,
                SystemMonitorNames.Function,
                observer.Process.Pid,
                Term.List(Term.Tuple(Term.A(SystemMonitorNames.LongQueue), Term.Tuple(Term.I(0), Term.I(1))))
            ));
            Term table = await giver.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.New,
                Term.A(EtsHeir419HostNames.Table),
                Term.List(Term.A(EtsHeir419HostNames.Named), Term.A(EtsHeir419HostNames.Public))
            ));
            object gate = typeof(ProcessRuntime).GetField(EtsHeir419HostNames.Gate, Members)!.GetValue(runtime)!;
            PropertyInfo property = typeof(Mailbox).GetProperty(EtsHeir419HostNames.QueueChanged, Members)!;
            var original = (Action<int>?)property.GetValue(target.Mailbox);
            using var release = new ManualResetEventSlim();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int intercepted = 0;
            property.SetValue(
                target.Mailbox,
                (Action<int>)(count =>
            {
                if (count == 1 && Interlocked.CompareExchange(ref intercepted, 1, 0) == 0)
                {
                    Require(!System.Threading.Monitor.IsEntered(gate));
                    entered.TrySetResult();
                    Require(release.Wait(EtsHeir419HostNames.Deadline));
                }
                original?.Invoke(count);
            })
            );
            Task<Term>? giving = null;
            Task<Term?>? reading = null;
            var failures = new List<Exception>();
            try
            {
                giving = giver.InvokeAsync(async c => await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Give,
                    table,
                    receiver.Process.Pid,
                    Term.I(EtsHeir419HostNames.Value)
                ));
                await entered.Task.WaitAsync(EtsHeir419HostNames.Deadline);
                if (terminate)
                {
                    await observer.Invoke(c =>
               {
                   runtime.Exit(c, receiver.Process.Pid, Term.A(EtsHeir419HostNames.Kill));

                   return true;
               });
                    Require(!receiver.Process.Completion.IsCompleted);
                }
                else
                {
                    var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    // Real public thread-safe mailbox receive, no fake queue mutation/owner setter.
                    reading = Task.Run(async () =>
                    {
                        started.TrySetResult();

                        return await target.Mailbox.ReceiveAsync<Term>(
                            value => value is TupleTerm { Items.Count: 4 } t && t.Items[0].Equals(Term.A(EtsHeir419HostNames.Transfer)) ? value : null,
                            TimeSpan.Zero,
                            target.Cancellation
                        );
                    });
                    await started.Task.WaitAsync(EtsHeir419HostNames.Deadline);
                    Require(!reading.IsCompleted);
                }
                release.Set();
                await giving.WaitAsync(EtsHeir419HostNames.Deadline);
                if (terminate) await receiver.Process.Completion.WaitAsync(EtsHeir419HostNames.Deadline);
                else
                {
                    Require(await reading!.WaitAsync(EtsHeir419HostNames.Deadline) is TupleTerm { Items.Count: 4 });
                    await observer.InvokeAsync(async c =>
                    {
                        for (int expected = 1; expected >= 0; expected--)
                        {
                            Term? observed = await c.Mailbox.ReceiveAsync<Term>(
                                value => value is TupleTerm { Items.Count: 4 } t && t.Items[0].Equals(Term.A(SystemMonitorNames.Notification)) && t.Items[1].Equals(receiver.Process.Pid) && t.Items[2].Equals(Term.A(SystemMonitorNames.LongQueue)) ? value : null,
                                EtsHeir419HostNames.Deadline,
                                c.Cancellation
                            );
                            Require(observed is TupleTerm notification && notification.Items[3].Equals(Term.A(expected == 1 ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)));
                        }

                        return true;
                    });
                    Require(target.Mailbox.Count == 0);
                    _ = await receiver.InvokeAsync(async c => await Call(
                        runtime,
                        c,
                        EtsHeir419HostNames.Delete,
                        table
                    ));
                }
                object monitor = typeof(ProcessRuntime).GetProperty(EtsHeir419HostNames.MonitorProperty, Members)!.GetValue(runtime)!;
                object eventGate = monitor.GetType().GetField(EtsHeir419HostNames.MonitorGate, Members)!.GetValue(monitor)!;
                lock (eventGate) Require(!((HashSet<Pid>)monitor.GetType().GetField(EtsHeir419HostNames.HighQueues, Members)!.GetValue(monitor)!).Contains(receiver.Process.Pid));
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            finally
            {
                release.Set();
                property.SetValue(target.Mailbox, original);
                if (giving is not null) try
                    {
                        await giving.WaitAsync(EtsHeir419HostNames.Deadline);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
                if (reading is not null) try
                    {
                        await reading.WaitAsync(EtsHeir419HostNames.Deadline);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
            }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
    }
}
