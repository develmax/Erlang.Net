using System.Diagnostics;

namespace Erlang.Otp;

public enum RestartStrategy { OneForOne, OneForAll, RestForOne }
public enum RestartPolicy { Permanent, Transient, Temporary }
public sealed record ChildSpec(Term Id, Func<ProcessContext, ValueTask<ProcessHandle>> Start, RestartPolicy Restart = RestartPolicy.Permanent, TimeSpan? Shutdown = null);
public sealed class SupervisorHandle
{
    private (Term Id, Pid Pid)[] children = [];
    public ProcessHandle Process { get; internal set; } = null!;
    public IReadOnlyList<(Term Id, Pid Pid)> Children => Array.AsReadOnly(Volatile.Read(ref children));
    internal void Publish(IEnumerable<(Term Id, Pid Pid)> current) => Volatile.Write(ref children, current.ToArray());
}
public static class Supervisor
{
    private sealed record Child(ChildSpec Spec, ProcessHandle Process);
    public static async ValueTask<SupervisorHandle> Start(ProcessRuntime runtime, IReadOnlyList<ChildSpec> specifications, RestartStrategy strategy = RestartStrategy.OneForOne, int intensity = 1, TimeSpan? period = null, ProcessContext? parent = null)
    {
        if (intensity < 0 || period <= TimeSpan.Zero || specifications.Select(s => s.Id).Distinct().Count() != specifications.Count) throw new ErlangException("badarg");
        var handle = new SupervisorHandle(); var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        handle.Process = runtime.Spawn(async context =>
        {
            context.TrapExits = true; var children = new Child?[specifications.Count]; var restarts = new Queue<long>(); var retired = new HashSet<Pid>();
            void Publish() => handle.Publish(children.OfType<Child>().Select(c => (c.Spec.Id, c.Process.Pid)));
            async ValueTask StartChild(int index)
            {
                var process = await specifications[index].Start(context);
                if (process.Completion.IsCompleted) throw new ErlangException(Term.Tuple(Term.A("failed_to_start_child"), specifications[index].Id));
                runtime.Link(context, process.Pid); children[index] = new(specifications[index], process); Publish();
            }
            async ValueTask StopChild(int index)
            {
                if (children[index] is not { } child) return; retired.Add(child.Process.Pid); children[index] = null; Publish();
                runtime.Exit(context, child.Process.Pid, Term.A("shutdown"));
                try { await child.Process.Completion.WaitAsync(child.Spec.Shutdown ?? TimeSpan.FromSeconds(5)); }
                catch (TimeoutException) { runtime.Exit(context, child.Process.Pid, Term.A("kill")); await child.Process.Completion; }
            }
            try
            {
                for (int i = 0; i < children.Length; i++) await StartChild(i); ready.TrySetResult();
                while (true)
                {
                    var message = (await context.ReceiveAsync(t => t))!;
                    if (message.Equals(Term.A("$supervisor_stop"))) { for (int i = children.Length - 1; i >= 0; i--) await StopChild(i); context.Exit(Term.A("shutdown")); }
                    if (message is not TupleTerm { Items.Count: 3 } exit || !exit.Items[0].Equals(Term.A("EXIT")) || exit.Items[1] is not Pid pid) continue;
                    if (retired.Remove(pid)) continue;
                    int failed = Array.FindIndex(children, c => c is not null && c.Process.Pid.Equals(pid));
                    if (failed < 0) { if (parent is not null && pid.Equals(parent.Self)) context.Exit(exit.Items[2]); continue; }
                    var spec = children[failed]!.Spec; children[failed] = null; Publish();
                    bool normal = exit.Items[2].Equals(Term.A("normal")) || exit.Items[2].Equals(Term.A("shutdown")) || exit.Items[2] is TupleTerm { Items.Count: 2 } reason && reason.Items[0].Equals(Term.A("shutdown"));
                    if (spec.Restart == RestartPolicy.Temporary || spec.Restart == RestartPolicy.Transient && normal) continue;
                    long now = Stopwatch.GetTimestamp(); while (restarts.Count > 0 && Stopwatch.GetElapsedTime(restarts.Peek(), now) > (period ?? TimeSpan.FromSeconds(5))) restarts.Dequeue(); restarts.Enqueue(now);
                    if (restarts.Count > intensity) context.Exit(Term.A("shutdown"));
                    int first = strategy == RestartStrategy.OneForAll ? 0 : failed; int last = strategy == RestartStrategy.OneForOne ? failed : children.Length - 1;
                    for (int i = last; i >= first; i--) if (i != failed) await StopChild(i);
                    for (int i = first; i <= last; i++) if (specifications[i].Restart != RestartPolicy.Temporary) await StartChild(i);
                }
            }
            catch (Exception ex) { ready.TrySetException(ex); throw; }
            finally { if (context.ExitReason is null) for (int i = children.Length - 1; i >= 0; i--) await StopChild(i); Publish(); }
        }, parent, parent is not null);
        await ready.Task; return handle;
    }
    public static async ValueTask Stop(ProcessRuntime runtime, SupervisorHandle supervisor)
    { runtime.Send(supervisor.Process.Pid, Term.A("$supervisor_stop")); await supervisor.Process.Completion; }
}
