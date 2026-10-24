using System.Linq.Expressions;
using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsHeir419DeferredChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var recipient = new RuntimeNodeFoundationHostActor(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        _ = await observer.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            ErlangModuleNames.Module,
            SystemMonitorNames.Function,
            observer.Process.Pid,
            Term.List(Term.Tuple(Term.A(SystemMonitorNames.LongQueue), Term.Tuple(Term.I(1), Term.I(2))))
        ));
        await recipient.Invoke(c =>
        {
            var box = c.Mailbox;
            object gate = typeof(ProcessRuntime).GetField(EtsHeir419HostNames.Gate, Members)!.GetValue(runtime)!;
            var property = typeof(Mailbox).GetProperty(EtsHeir419HostNames.QueueChanged, Members)!;
            var original = (Action<int>?)property.GetValue(box);
            var counts = new List<int>();
            int released = 0;
            property.SetValue(box, (Action<int>)(count =>
   {
       counts.Add(count);
       original?.Invoke(count);
   }));
            object? admission = null;
            try
            {
                // Invoke real prepared queue kernel, not a fabricated node/count/owner setter.
                // Ownership publication is independently covered by the native AtomicQueue HOST.
                admission = EtsFoundation405Access.Invoke(
                    box,
                    EtsHeir419HostNames.PrepareTransfer,
                    c,
                    Term.Tuple(Term.A(EtsHeir419HostNames.Transfer), Term.I(EtsHeir419HostNames.Value)),
                    observer.Process.Pid
                )!;
                lock (gate)
                {
                    EtsFoundation405Access.Invoke(admission, EtsHeir419HostNames.Validate, box);
                    EtsFoundation405Access.Invoke(admission, EtsHeir419HostNames.Track, c);
                    EtsFoundation405Access.Invoke(admission, EtsHeir419HostNames.Commit, box);
                }
                Type type = typeof(ProcessRuntime).Assembly.GetType(EtsHeir419HostNames.DeferredType, true)!;
                object deferred = Activator.CreateInstance(type, Term.I(EtsHeir419HostNames.Value), false)!;
                Delegate prepare = Expression.Lambda(typeof(Func<>).MakeGenericType(type), Expression.Constant(deferred, type)).Compile();
                EtsFoundation405Access.Invoke(
                    box,
                    EtsHeir419HostNames.SendDeferred,
                    prepare,
                    (Action)(() => released++),
                    null,
                    null
                );
                object claim = EtsFoundation405Access.Invoke(box, EtsHeir419HostNames.ClaimDeferred)!;
                Delegate get = (Delegate)claim.GetType().GetProperty(EtsHeir419HostNames.Prepare)!.GetValue(claim)!;
                Delegate complete = (Delegate)claim.GetType().GetProperty(EtsHeir419HostNames.Complete)!.GetValue(claim)!;
                complete.DynamicInvoke(get.DynamicInvoke());
                EtsFoundation405Access.Require(counts.SequenceEqual(new[] { 1, 2 }) && released == 1 && box.Count == 2);
                EtsFoundation405Access.Invoke(admission, EtsHeir419HostNames.Notify);
                admission = null;
                EtsFoundation405Access.Require(counts.SequenceEqual(new[] { 1, 2 }));
                object monitor = typeof(ProcessRuntime).GetProperty(EtsHeir419HostNames.MonitorProperty, Members)!.GetValue(runtime)!;
                object eventGate = monitor.GetType().GetField(EtsHeir419HostNames.MonitorGate, Members)!.GetValue(monitor)!;
                lock (eventGate) EtsFoundation405Access.Require(((HashSet<Pid>)monitor.GetType().GetField(EtsHeir419HostNames.HighQueues, Members)!.GetValue(monitor)!).Contains(c.Self));
            }
            finally
            {
                try
                {
                    if (admission is not null) EtsFoundation405Access.Invoke(admission, EtsHeir419HostNames.Notify);
                }
                finally
                {
                    property.SetValue(box, original);
                }
            }

            return true;
        });
    }
}
