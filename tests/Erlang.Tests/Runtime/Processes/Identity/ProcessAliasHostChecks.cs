using Erlang;
using System.Threading.Channels;

namespace Erlang.Tests;

internal static class ProcessAliasHostChecks
{
    public static async Task Mailbox()
    {
        var mailbox = new Mailbox();
        mailbox.Send(Term.I(10));
        mailbox.Send(Term.I(1), true);
        mailbox.Send(Term.I(2), true);
        mailbox.Send(Term.I(20));
        if (mailbox.Remove(term => term.Equals(Term.I(2))) != 1)
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Mailbox);
        mailbox.Send(Term.I(3), true);
        if (mailbox.Remove(term => term.Equals(Term.I(1))) != 1)
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Mailbox);
        mailbox.Send(Term.I(4), true);
        foreach (int expected in new[] { 3, 4, 10, 20 })
        {
            Term? actual = await mailbox.ReceiveAsync<Term>(term => term, TimeSpan.Zero);
            if (!Term.I(expected).Equals(actual))
                throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Mailbox);
        }
        mailbox.Send(Term.I(30));
        mailbox.Send(Term.I(5), true);
        mailbox.Send(Term.I(6), true);
        if (mailbox.Remove(_ => true) != 3 || mailbox.Count != 0)
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Mailbox);
        mailbox.Send(Term.I(40));
        mailbox.Send(Term.I(7), true);
        if (!Term.I(7).Equals(await mailbox.ReceiveAsync<Term>(term => term, TimeSpan.Zero)))
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Mailbox);
        var waiting = mailbox.ReceiveAsync<Term>(term => term.Equals(Term.I(8)) ? term : null, null).AsTask();
        mailbox.Send(Term.I(8), true);
        if (!Term.I(8).Equals(await waiting.WaitAsync(TimeSpan.FromSeconds(ProcessAliasCheckParameters.TimeoutSeconds))) || mailbox.Count != 1)
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Mailbox);
    }

    public static async Task Lifecycle()
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<(ProcessContext Context, ReferenceTerm Alias)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            ReferenceTerm alias = runtime.CreateAlias(context, true, true);
            ready.SetResult((context, alias));
            await release.Task.WaitAsync(context.Cancellation);

            return Term.I(0);
        });
        var owner = await ready.Task.WaitAsync(TimeSpan.FromSeconds(ProcessAliasCheckParameters.TimeoutSeconds));
        ReferenceTerm reconstructed = (ReferenceTerm)ExternalTermFormat.Decode(ExternalTermFormat.Encode(owner.Alias));
        Parallel.For(0, ProcessAliasCheckParameters.ConcurrentReplies, index => runtime.Send(reconstructed, Term.I(index), true));
        if (owner.Context.Mailbox.Count != 1 || runtime.Unalias(owner.Context, reconstructed))
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Lifecycle);
        ReferenceTerm explicitAlias = runtime.CreateAlias(owner.Context, false, false);
        runtime.Send(explicitAlias, Term.I(1000));
        if (owner.Context.Mailbox.Count != 2)
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Lifecycle);
        if (!runtime.Unalias(owner.Context, explicitAlias) || owner.Context.Mailbox.Count != 2)
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Lifecycle);
        ReferenceTerm liveAtExit = runtime.CreateAlias(owner.Context, false, true);
        release.SetResult();
        await process.Completion.WaitAsync(TimeSpan.FromSeconds(ProcessAliasCheckParameters.TimeoutSeconds));
        runtime.Send(liveAtExit, Term.I(2000), true);
        if (owner.Context.Mailbox.Count != 2 || runtime.Unalias(owner.Context, liveAtExit))
            throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Lifecycle);
    }

    public static async Task PendingWakeups()
    {
        await using var runtime = new ProcessRuntime();
        var aliases = Channel.CreateUnbounded<ReferenceTerm>();
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                for (int index = 0; index < ProcessAliasCheckParameters.WakeIterations; index++)
                {
                    ReferenceTerm alias = runtime.CreateAlias(context, true, false);
                    await aliases.Writer.WriteAsync(alias);
                    Term? received = await context.ReceiveAsync<Term>(term => term, TimeSpan.FromSeconds(ProcessAliasCheckParameters.TimeoutSeconds));
                    if (!Term.I(index).Equals(received) || runtime.Unalias(context, alias))
                        throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Lifecycle);
                }
                if (context.Mailbox.Count != 0)
                    throw new InvalidOperationException(ProcessAliasCheckDiagnostics.Lifecycle);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                aliases.Writer.TryComplete();
            }

            return Term.I(0);
        });
        for (int index = 0; index < ProcessAliasCheckParameters.WakeIterations; index++)
        {
            ReferenceTerm alias = await aliases.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(ProcessAliasCheckParameters.TimeoutSeconds));
            runtime.Send(alias, Term.I(index));
            runtime.Send(alias, Term.I(-index - 1));
        }
        await process.Completion.WaitAsync(TimeSpan.FromSeconds(ProcessAliasCheckParameters.TimeoutSeconds));
        if (failure is not null)
            throw failure;
    }
}
