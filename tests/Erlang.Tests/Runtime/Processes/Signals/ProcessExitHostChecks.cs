using Erlang;
using System.Collections.Concurrent;

namespace Erlang.Tests;

internal static class ProcessExitHostChecks
{
    public static async Task PendingKillCleanup()
    {
        await using var runtime = new ProcessRuntime();
        var contexts = new ConcurrentBag<ProcessContext>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = runtime.Spawn(async parent =>
        {
            var children = new List<ProcessHandle>();
            for (int index = 0; index < ProcessExitCheckParameters.ConcurrentChildren; index++)
                children.Add(runtime.Spawn(
                    async child =>
                {
                    contexts.Add(child);
                    Term alias = await runtime.Modules.Call(child, ErlangModuleNames.Module, ProcessExitNamesForChecks.Alias);
                    runtime.Send(parent.Self, alias);
                    await release.Task;
                    await child.ReceiveAsync(term => term, ProcessExitCheckParameters.CompletionTimeout);

                    return Nil.Value;
                },
                    parent
                ));
            var aliases = new List<ReferenceTerm>();
            for (int index = 0; index < ProcessExitCheckParameters.ConcurrentChildren; index++)
                aliases.Add((await parent.ReceiveAsync(term => term as ReferenceTerm, ProcessExitCheckParameters.CompletionTimeout))!);
            foreach (ReferenceTerm alias in aliases)
            {
                for (int signal = 0; signal < ProcessExitHostParameters.PendingKillSignalsPerChild; signal++)
                {
                    Term reply = await runtime.Modules.Call(
                        parent,
                        ErlangModuleNames.Module,
                        ProcessExitNamesForChecks.ExitSignal,
                        alias,
                        Term.A(ProcessExitSignals.Kill)
                    );
                    if (!reply.Equals(Term.A(ErlangBooleanAtoms.True)))
                        throw new InvalidOperationException(ProcessExitCheckDiagnostics.WrongReply);
                }
                runtime.Send(alias, Term.I(ProcessExitHostParameters.DiscardedPayload));
            }
            release.TrySetResult();
            Term[] completions = await Task.WhenAll(children.Select(child => child.Completion)).WaitAsync(ProcessExitCheckParameters.CompletionTimeout);
            if (completions.Any(reason => !reason.Equals(Term.A(ProcessExitReasons.Killed))))
                throw new InvalidOperationException(ProcessExitCheckDiagnostics.WrongCompletion);
            if (children.Any(child => runtime.IsAlive(child.Pid)))
                throw new InvalidOperationException(ProcessExitCheckDiagnostics.AliveChild);
            if (contexts.Any(child => child.Mailbox.Count != 0))
                throw new InvalidOperationException(ProcessExitCheckDiagnostics.InvalidPendingCount);

            return Nil.Value;
        });
        Term completion = await owner.Completion.WaitAsync(ProcessExitCheckParameters.CompletionTimeout);
        if (!completion.Equals(Term.A(ProcessExitReasons.Normal)))
            throw new InvalidOperationException(completion.ToString());
    }
}
