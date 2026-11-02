using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxReentrantExit606Names;

namespace Erlang.Tests;

internal static class MailboxReentrantExit606Checks
{
    internal static async Task Pending()
    {
        var failures = new List<Exception>();
        foreach (bool bounded in new[] { false, true })
            foreach (bool? priority in new bool?[] { null, false, true })
            {
                await InRuntime(
                    async (
                        runtime,
                        actor,
                        context,
                        mailbox
                    ) =>
                {
                    int admitted = 0, later = 0;
                    await actor.Invoke(active =>
                    {
                        lock (RuntimeMonitor(runtime))
                        {
                            runtime.Send(active.Self, Term.I(ExistingMessage));
                            QueuePending(
                                mailbox,
                                () =>
                            {
                                Interlocked.Increment(ref admitted);
                                runtime.Exit(active, active.Self, Term.A(Kill));

                                return priority;
                            },
                                active.Self
                            );
                            QueuePending(mailbox, () =>
     {
         Interlocked.Increment(ref later);

         return null;
     }, active.Self);
                            if (bounded)
                                _ = Call(mailbox, AdmitBounded, SignalBudget);
                            else
                                Call(mailbox, AdmitPending);
                        }

                        return Nil.Value;
                    }).WaitAsync(Deadline);
                    Term reason = await actor.Process.Completion.WaitAsync(Deadline);
                    Require(reason.Equals(Term.A(Killed)) && !runtime.IsAlive(context.Self));
                    Require(admitted == 1 && later == 0);
                    RequireEmpty(mailbox);
                },
                    failures
                );
            }
        Finish(failures);
    }

    internal static async Task Deferred()
    {
        var failures = new List<Exception>();
        foreach (bool discard in new[] { false, true })
        {
            await InRuntime(
                async (
                    runtime,
                    actor,
                    context,
                    mailbox
                ) =>
            {
                int prepared = 0, released = 0;
                var observed = new List<Exception>();
                try
                {
                    await actor.Invoke(active =>
                    {
                        Delegate prepare = NullPreparation(() => Interlocked.Increment(ref prepared));
                        Action release = () =>
                        {
                            Interlocked.Increment(ref released);
                            runtime.Exit(active, active.Self, Term.A(Kill));
                        };
                        if (discard)
                        {
                            lock (RuntimeMonitor(runtime))
                            {
                                Call(
                                    mailbox,
                                    SendDeferred,
                                    prepare,
                                    release,
                                    active.Self,
                                    null
                                );
                                Call(mailbox, DiscardPending);
                            }
                        }
                        else
                        {
                            object work;
                            lock (RuntimeMonitor(runtime))
                            {
                                Call(
                                    mailbox,
                                    SendDeferred,
                                    prepare,
                                    release,
                                    active.Self,
                                    null
                                );
                                work = Call(mailbox, ClaimDeferred) ?? throw new InvalidOperationException(Failure);
                            }
                            object? result = Invoke((Delegate)Property(work, Prepare));
                            lock (RuntimeMonitor(runtime))
                                Invoke((Delegate)Property(work, Complete), result);
                        }

                        return Nil.Value;
                    }).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                try
                {
                    Term reason = await actor.Process.Completion.WaitAsync(Deadline);
                    Require(reason.Equals(Term.A(Killed)) && !runtime.IsAlive(context.Self));
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                try
                {
                    Require(prepared == (discard ? 0 : 1) && released == 1);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                try
                {
                    RequireEmpty(mailbox);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                Finish(observed);
            },
                failures
            );
        }
        await CopyCallbackClose606(failures);
        Finish(failures);
    }

    private static async Task CopyCallbackClose606(List<Exception> failures)
    {
        foreach (bool directHalt in new[] { false, true })
        {
            await InRuntime(
                async (
                    runtime,
                    actor,
                    context,
                    mailbox
                ) =>
            {
                int prepared = 0, released = 0;
                var counts = new List<int>();
                var observed = new List<Exception>();
                PropertyInfo observer = mailbox.GetType().GetProperty(QueueLengthObserver, Members)!;
                var previous = (Action<int>?)observer.GetValue(mailbox);
                try
                {
                    await actor.Invoke(active =>
                    {
                        Action<int> callback = count =>
                        {
                            previous?.Invoke(count);
                            counts.Add(count);
                            if (count == CloseAtCopy)
                            {
                                if (directHalt) Call(mailbox, StopForHalt);
                                runtime.Exit(active, active.Self, Term.A(Kill));
                            }
                        };
                        observer.SetValue(mailbox, callback);
                        Type admissionType = typeof(ProcessRuntime).Assembly.GetType(DeferredAdmissionType, true)!;
                        object admission = Activator.CreateInstance(
                            admissionType,
                            Members,
                            null,
                            [Term.I(ExistingMessage), false],
                            null
                        )!;
                        admissionType.GetProperty(Copies, Members)!.SetValue(admission,
                            new System.Numerics.BigInteger(RequestedCopies));
                        Delegate prepare = (Delegate)typeof(MailboxReentrantExit606Checks)
                            .GetMethod(nameof(PreparationValue606), Members)!.MakeGenericMethod(admissionType)
                            .Invoke(null, [admission, (Action)(() => Interlocked.Increment(ref prepared))])!;
                        Action release = () => Interlocked.Increment(ref released);
                        object work;
                        lock (RuntimeMonitor(runtime))
                        {
                            Call(
                                mailbox,
                                SendDeferred,
                                prepare,
                                release,
                                active.Self,
                                null
                            );
                            work = Call(mailbox, ClaimDeferred) ?? throw new InvalidOperationException(Failure);
                        }
                        object? result = Invoke((Delegate)Property(work, Prepare));
                        lock (RuntimeMonitor(runtime))
                            Invoke((Delegate)Property(work, Complete), result);

                        return Nil.Value;
                    }).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                finally
                {
                    try
                    {
                        observer.SetValue(mailbox, previous);
                    }
                    catch (Exception error)
                    {
                        Console.Error.WriteLine(error);
                        observed.Add(error);
                    }
                }
                try
                {
                    Term reason = await actor.Process.Completion.WaitAsync(Deadline);
                    Require(reason.Equals(Term.A(Killed)) && !runtime.IsAlive(context.Self));
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                try
                {
                    Require(prepared == 1 && released == 1 && counts.SequenceEqual(new[] { 1, CloseAtCopy }));
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                try
                {
                    RequireEmpty(mailbox);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    observed.Add(error);
                }
                Finish(observed);
            },
                failures
            );
        }
    }

    private static Delegate PreparationValue606<T>(object value, Action entered) where T : class
        => new Func<T>(() =>
 {
     entered();

     return (T)value;
 });

    internal static async Task Late()
    {
        var failures = new List<Exception>();
        await InRuntime(
            async (
                runtime,
                actor,
                context,
                mailbox
            ) =>
        {
            await actor.Invoke(active =>
            {
                runtime.Exit(active, active.Self, Term.A(Kill));

                return Nil.Value;
            }).WaitAsync(Deadline);
            Term reason = await actor.Process.Completion.WaitAsync(Deadline);
            Require(reason.Equals(Term.A(Killed)) && !runtime.IsAlive(context.Self));
            int prepared = 0, released = 0;
            bool underMailbox = false, underRuntime = false;
            object runtimeGate = RuntimeMonitor(runtime);
            Delegate prepare = NullPreparation(() => Interlocked.Increment(ref prepared));
            Action release = () =>
            {
                Interlocked.Increment(ref released);
                underMailbox |= (bool)Property(mailbox, DeferredGateHeld);
                underRuntime |= Monitor.IsEntered(runtimeGate);
            };
            // This actual late producer targets the existing, physically closed
            // mailbox. Runtime remains live; HaltRequested is not the boundary.
            Call(
                mailbox,
                SendDeferred,
                prepare,
                release,
                context.Self,
                null
            );
            Require(prepared == 0 && released == 1 && !underMailbox && !underRuntime);
            RequireEmpty(mailbox);
        },
            failures
        );
        Finish(failures);
    }

    private static void QueuePending(object mailbox, Func<bool?> admit, Pid sender)
        => Call(
            mailbox,
            SendPending,
            Nil.Value,
            admit,
            null,
            false,
            sender,
            false,
            null,
            null,
            false
        );

    private static object RuntimeMonitor(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetField(RuntimeGate, Members)!.GetValue(runtime)!;

    private static Delegate NullPreparation(Action entered)
    {
        Type admission = typeof(ProcessRuntime).Assembly.GetType(DeferredAdmissionType, true)!;

        return (Delegate)typeof(MailboxReentrantExit606Checks)
            .GetMethod(nameof(NullPreparationTyped), Members)!.MakeGenericMethod(admission)
            .Invoke(null, [entered])!;
    }

    private static Delegate NullPreparationTyped<T>(Action entered) where T : class
        => new Func<T?>(() =>
 {
     entered();

     return null;
 });

    private static object? Invoke(Delegate body, params object?[] arguments)
    {
        try
        {
            return body.DynamicInvoke(arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void RequireEmpty(object mailbox)
    {
        Type type = mailbox.GetType();
        Require((int)type.GetField(MailboxReentrantExit606Names.Pending, Members)!.GetValue(mailbox)! == 0
            && (int)type.GetField(DeferredPending, Members)!.GetValue(mailbox)! == 0
            && (int)Property(mailbox, Admitted) == 0
            && !(bool)Property(mailbox, HasPendingSignals)
            && !(bool)Property(mailbox, HasDeferredPending)
            && ((IReadOnlyList<Term>)Call(mailbox, Snapshot)!).Count == 0);
    }

    private static async Task InRuntime(
        Func<ProcessRuntime, RuntimeNodeFoundationHostActor, ProcessContext, object, Task> body,
        List<Exception> failures)
    {
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            actor = new(runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            object mailbox = Property(context, MailboxReentrantExit606Names.Mailbox);
            await body(
                runtime,
                actor,
                context,
                mailbox
            );
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
