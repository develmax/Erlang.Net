using Erlang;

namespace Erlang.Tests;

internal static class ProcessMonitorHostChecks
{
    public static async Task ConcurrentTermination()
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = runtime.Spawn(async context =>
        {
            ready.SetResult(context);
            await release.Task.WaitAsync(context.Cancellation);

            return Term.I(0);
        });
        ProcessContext observer = await ready.Task.WaitAsync(TimeSpan.FromSeconds(ProcessMonitorCheckParameters.TimeoutSeconds));
        var stop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var children = Enumerable.Range(0, ProcessMonitorCheckParameters.Children).Select(_ => runtime.Spawn(async context =>
        {
            await stop.Task.WaitAsync(context.Cancellation);

            return Term.I(0);
        })).ToArray();
        var references = new ReferenceTerm[children.Length];
        for (int index = 0; index < children.Length; index++)
            references[index] = runtime.Monitor(observer, children[index].Pid);
        Parallel.For(0, children.Length / 2, index => runtime.Demonitor(observer, references[index]));
        stop.SetResult();
        await Task.WhenAll(children.Select(child => child.Completion)).WaitAsync(TimeSpan.FromSeconds(ProcessMonitorCheckParameters.TimeoutSeconds));
        var seen = new HashSet<ReferenceTerm>();
        for (int index = children.Length / 2; index < children.Length; index++)
        {
            Term? message = await observer.Mailbox.ReceiveAsync<Term>(term => term, TimeSpan.FromSeconds(ProcessMonitorCheckParameters.TimeoutSeconds));
            if (message is not TupleTerm { Items.Count: 5 } notification || notification.Items[1] is not ReferenceTerm reference || !references.Skip(children.Length / 2).Contains(reference) || !seen.Add(reference))
                throw new InvalidOperationException(ProcessMonitorCheckDiagnostics.Termination);
            int targetIndex = Array.IndexOf(references, reference);
            if (!notification.Equals(Term.Tuple(
                Term.A(ProcessMonitorCheckNames.Down),
                reference,
                Term.A(ProcessMonitorCheckNames.Process),
                children[targetIndex].Pid,
                Term.A(ProcessMonitorCheckNames.Normal)
            )))
                throw new InvalidOperationException(ProcessMonitorCheckDiagnostics.Termination);
        }
        if (observer.Mailbox.Count != 0 || seen.Count != children.Length / 2)
            throw new InvalidOperationException(ProcessMonitorCheckDiagnostics.Termination);
        release.SetResult();
        await owner.Completion.WaitAsync(TimeSpan.FromSeconds(ProcessMonitorCheckParameters.TimeoutSeconds));
    }
}
