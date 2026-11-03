using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

public static class DeferredMailboxFailureChecks
{
    public static Task Prepare() => Run(prepareFails: true, releaseFails: true);

    public static Task Release() => Run(prepareFails: false, releaseFails: true);

    public static Task Complete() => Run(prepareFails: false, releaseFails: false);

    private static async Task Run(bool prepareFails, bool releaseFails)
    {
        foreach (ProcessMessageQueueDataMode411 mode in Enum.GetValues<ProcessMessageQueueDataMode411>())
        {
            await using var runtime = new ProcessRuntime(2);
            var prepareError = new InvalidOperationException();
            var releaseError = new InvalidOperationException();
            var completeError = new InvalidOperationException();
            Exception? failure = null;
            bool completed = false;
            ProcessHandle actor = runtime.Spawn(context =>
            {
                try
                {
                    context.Mailbox.ExchangeMessageQueueData411(mode);
                    int prepares = 0, releases = 0, completions = 0;
                    Term? physical = null;
                    var work = new MailboxDeferredPreparation(
                        () =>
                        {
                            prepares++;
                            if (prepareFails) throw prepareError;

                            return new DeferredMailboxAdmission(Term.Tuple(Term.I(7)));
                        },
                        value =>
                        {
                            completions++;
                            if (prepareFails || releaseFails)
                                Require(value is null);
                            else
                            {
                                Require(value is not null);
                                physical = value!.Message;
                                Require(MailboxBackingProbe.Storage(physical) is not null);
                            }
                            throw completeError;
                        },
                        () =>
                        {
                            releases++;
                            if (releaseFails) throw releaseError;
                        },
                        AllowBacking: true
                    );
                    Exception? actual = null;
                    try
                    {
                        typeof(ProcessRuntime).GetMethod(
                            DeferredMailboxFailureNames.Method,
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )!.Invoke(runtime, [context, work]);
                    }
                    catch (TargetInvocationException error) when (error.InnerException is not null)
                    {
                        actual = error.InnerException;
                    }
                    Require(prepares == 1 && releases == 1 && completions == 1);
                    Exception[] expected = prepareFails ? [prepareError, releaseError, completeError]
                        : releaseFails ? [releaseError, completeError] : [completeError];
                    Exception[] errors = actual is AggregateException aggregate ? aggregate.InnerExceptions.ToArray()
                        : actual is null ? [] : [actual];
                    Require(errors.Length == expected.Length);
                    for (int index = 0; index < errors.Length; index++)
                        Require(ReferenceEquals(errors[index], expected[index]));
                    if (physical is not null)
                    {
                        var storage = MailboxBackingProbe.Storage(physical)!;
                        if (mode == ProcessMessageQueueDataMode411.OffHeap)
                            Require(((ProcessTermHeap506)storage.Owner).Snapshot.CapacityWords == 0);
                        else
                            _ = context.CollectOwnedGeneration508(true);
                        bool retired = false;
                        try
                        {
                            _ = ((TupleTerm)physical).Items[0];
                        }
                        catch (ObjectDisposedException)
                        {
                            retired = true;
                        }
                        Require(retired);
                        GC.KeepAlive(physical);
                    }
                    completed = true;
                }
                catch (Exception error)
                {
                    failure = error;
                }

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await actor.Completion.WaitAsync(DeferredMailboxFailureNames.Deadline);
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
            Require(completed);
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(DeferredMailboxFailureNames.Error);
    }
}
