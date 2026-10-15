using Erlang;
using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class AllocatorQueryMailboxHostChecks
{
    internal static async Task Run()
    {
        await ValidationAndReferenceOrder();
        await OrderedSelectiveRetention();
        await DelayedReplyAndClosedSink();
        await CancelBeforeMailboxCommit();
        await RetireAndIsolateQueries();
        await StopOwner(false);
        await StopOwner(true);
        await ProducerFaultAndCounts();
        await MalformedAndZeroCount();
    }

    private static async Task ValidationAndReferenceOrder()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        int productions = 0;
        object provider = Provider(0, (
            _,
            _,
            _,
            _,
            _
        ) =>
        {
            productions++;

            return Task.CompletedTask;
        });
        FieldInfo counter = typeof(ProcessRuntime).GetField(
            AllocatorQueryMailboxHostNames.ReferenceCounterField, BindingFlags.NonPublic | BindingFlags.Instance)!;
        long before = (long)counter.GetValue(runtime)!;
        await Invalid(Term.I(0), ErlangErrorReasons.FunctionClause);
        Require((long)counter.GetValue(runtime)! == before);
        await Invalid(Nil.Value, ErlangErrorReasons.BadArgument);
        Require((long)counter.GetValue(runtime)! == before + 1);
        await Invalid(Term.A(AllocatorQueryMailboxHostNames.Unknown), ErlangErrorReasons.BadArgument);
        Require((long)counter.GetValue(runtime)! == before + 2 && productions == 0);
        using var retired = new CancellationTokenSource();
        retired.Cancel();
        object stopped = Provider(1, (
            _,
            _,
            _,
            _,
            _
        ) =>
        {
            productions++;

            return Task.CompletedTask;
        }, retired.Token);
        await Cancelled(owner.InvokeAsync(caller => Query(caller, stopped)));
        Require(productions == 0);

        async Task Invalid(Term input, string reason)
        {
            try
            {
                _ = await owner.InvokeAsync(caller => Query(caller, provider, input));
                throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A(reason)))
            {
            }
        }
    }

    private static async Task OrderedSelectiveRetention()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await owner.Ready;
        Term marker = Term.A(AllocatorQueryMailboxHostNames.Marker);
        Term wrongReference = Term.Tuple(runtime.MakeReference(), Nil.Value, Nil.Value);
        Term? wrongArity = null;
        Term? extra = null;
        context.Mailbox.Send(marker);
        context.Mailbox.Send(wrongReference);
        object provider = Provider(
            2,
            (
                caller,
                reference,
                _,
                post,
                _
            ) =>
        {
            wrongArity = Term.Tuple(reference, Nil.Value);
            extra = Term.Tuple(reference, marker, Nil.Value);
            caller.Mailbox.Send(wrongArity);
            post(Nil.Value, Information(12, Term.A(AllocatorQueryMailboxHostNames.Later)));
            post(Nil.Value, Information(2, Term.A(AllocatorQueryMailboxHostNames.Earlier)));
            caller.Mailbox.Send(extra);

            return Task.CompletedTask;
        }
        );
        Term result = await owner.InvokeAsync(caller => Query(caller, provider));
        Require(result.Equals(Term.List(
            Instance(2, Term.A(AllocatorQueryMailboxHostNames.Earlier)),
            Instance(12, Term.A(AllocatorQueryMailboxHostNames.Later)))));
        foreach (Term expected in new[] { marker, wrongReference, wrongArity!, extra! })
            Require((await owner.InvokeAsync(async caller => await caller.ReceiveAsync(value => value, TimeSpan.Zero)))!.Equals(expected));
        Require(context.Mailbox.Count == 0);
    }

    private static async Task DelayedReplyAndClosedSink()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await owner.Ready;
        var entered = Signal();
        var release = Signal();
        var ended = Signal();
        Action<Term, Term>? captured = null;
        object provider = Provider(
            1,
            async (
                _,
                _,
                _,
                post,
                stop
            ) =>
        {
            captured = post;
            entered.TrySetResult();
            try
            {
                await release.Task.WaitAsync(stop);
                post(Nil.Value, Information(0, Nil.Value));
            }
            finally
            {
                ended.TrySetResult();
            }
        }
        );
        Task<Term> query = owner.InvokeAsync(async caller =>
        {
            _ = await runtime.Modules.Call(
                caller,
                ErlangModuleNames.Module,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                Term.I(AllocatorQueryMailboxHostNames.TraceLabel)
            );
            Term result = await Query(caller, provider);
            Term label = await runtime.Modules.Call(
                caller,
                ErlangModuleNames.Module,
                SequenceTraceNames.Info,
                Term.A(SequenceTraceNames.Label)
            );
            Require(label.Equals(Term.Tuple(
                Term.A(SequenceTraceNames.Label), Term.I(AllocatorQueryMailboxHostNames.TraceLabel))));

            return result;
        });
        try
        {
            await entered.Task.WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            await Waiting(context);
            Require(!query.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }
        Require((await query).Equals(Term.List(Instance(0, Nil.Value))));
        Require(ended.Task.IsCompletedSuccessfully);
        try
        {
            captured!(Nil.Value, Nil.Value);
            throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
        }
        catch (InvalidOperationException failure) when (failure.Message != AllocatorQueryMailboxHostNames.Failure)
        {
            Require(context.Mailbox.Count == 0);
        }
    }

    private static async Task CancelBeforeMailboxCommit()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await owner.Ready;
        using var retirement = new CancellationTokenSource();
        var entered = Signal();
        var release = Signal();
        Term marker = Term.A(AllocatorQueryMailboxHostNames.Marker);
        context.Mailbox.Send(marker);
        Task<Term?> receive = owner.InvokeAsync(async caller => await ReceiveOperation(caller, value =>
        {
            entered.TrySetResult();
            release.Task.GetAwaiter().GetResult();

            return value;
        }, retirement.Token));
        try
        {
            await entered.Task.WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            await retirement.CancelAsync();
        }
        finally
        {
            release.TrySetResult();
        }
        await Cancelled(receive);
        Require(context.Mailbox.Count == 1);
        Require((await owner.InvokeAsync(async caller => await caller.ReceiveAsync(value => value, TimeSpan.Zero)))!.Equals(marker));
    }

    private static async Task RetireAndIsolateQueries()
    {
        await using var runtime = new ProcessRuntime();
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext firstContext = await first.Ready;
        ProcessContext secondContext = await second.Ready;
        using var retirement = new CancellationTokenSource();
        var firstEntered = Signal();
        var firstEnded = Signal();
        var secondEntered = Signal();
        var secondRelease = Signal();
        object firstProvider = Provider(
            1,
            async (
                _,
                _,
                _,
                _,
                stop
            ) =>
        {
            firstEntered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, stop);
            }
            finally
            {
                firstEnded.TrySetResult();
            }
        },
            retirement.Token
        );
        object secondProvider = Provider(
            1,
            async (
                caller,
                _,
                _,
                post,
                stop
            ) =>
        {
            secondEntered.TrySetResult();
            await secondRelease.Task.WaitAsync(stop);
            post(Nil.Value, Information(0, caller.Self));
        }
        );
        Task<Term> firstQuery = first.InvokeAsync(caller => Query(caller, firstProvider));
        Task<Term> secondQuery = second.InvokeAsync(caller => Query(caller, secondProvider));
        try
        {
            await Task.WhenAll(firstEntered.Task, secondEntered.Task).WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            await Waiting(firstContext);
            await Waiting(secondContext);
            await retirement.CancelAsync();
            await Cancelled(firstQuery);
            Require(firstEnded.Task.IsCompletedSuccessfully && !secondQuery.IsCompleted);
        }
        finally
        {
            await retirement.CancelAsync();
            secondRelease.TrySetResult();
        }
        Require((await secondQuery).Equals(Term.List(Instance(0, second.Process.Pid))));
    }

    private static async Task StopOwner(bool halt)
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await owner.Ready;
        var entered = Signal();
        var ended = Signal();
        using var retirement = new CancellationTokenSource();
        object provider = Provider(
            1,
            async (
                _,
                _,
                _,
                _,
                stop
            ) =>
        {
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, stop);
            }
            finally
            {
                ended.TrySetResult();
            }
        },
            retirement.Token
        );
        Task<Term> query = owner.InvokeAsync(caller => Query(caller, provider));
        try
        {
            await entered.Task.WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            await Waiting(context);
            if (halt)
                _ = runtime.RequestImmediateHalt(AllocatorQueryMailboxHostNames.HaltStatus);
            else
                await killer.Invoke(sender =>
                {
                    runtime.Exit(sender, owner.Process.Pid, Term.A(AllocatorQueryMailboxHostNames.Kill));

                    return true;
                });
            try
            {
                _ = await query;
                throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
            }
            catch (ProcessHaltTransfer transfer) when (halt && ReferenceEquals(transfer.Runtime, runtime))
            {
            }
            catch (OperationCanceledException) when (!halt)
            {
            }
            var unwind = (TaskCompletionSource)typeof(ProcessContext).GetProperty(
                AllocatorQueryMailboxHostNames.UnwoundProperty, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(context)!;
            await unwind.Task.WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            if (halt)
                await runtime.HaltQuiescence.WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            Require(ended.Task.IsCompletedSuccessfully);
        }
        finally
        {
            await retirement.CancelAsync();
        }
    }

    private static async Task ProducerFaultAndCounts()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await owner.Ready;
        var release = Signal();
        var primary = new InvalidDataException(AllocatorQueryMailboxHostNames.ProducerFailure);
        CancellationTokenRegistration registration = default;
        object provider = Provider(
            1,
            async (
                _,
                _,
                _,
                _,
                stop
            ) =>
        {
            registration = stop.Register(() => throw new InvalidOperationException(AllocatorQueryMailboxHostNames.CleanupFailure));
            await release.Task;
            throw primary;
        }
        );
        try
        {
            Task<Term> query = owner.InvokeAsync(caller => Query(caller, provider));
            await Waiting(context);
            release.TrySetResult();
            try
            {
                _ = await query;
                throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
            }
            catch (InvalidDataException failure) when (ReferenceEquals(failure, primary))
            {
                Require(failure.Data.Contains(AllocatorQueryMailboxHostNames.CleanupKey));
            }
        }
        finally
        {
            release.TrySetResult();
            registration.Dispose();
        }
        await ProviderViolation(Provider(1, (
            _,
            _,
            _,
            _,
            _
        ) => Task.CompletedTask));
        await ProviderViolation(Provider(1, (
            _,
            _,
            _,
            post,
            _
        ) =>
        {
            post(Nil.Value, Nil.Value);
            post(Nil.Value, Nil.Value);

            return Task.CompletedTask;
        }));

        async Task ProviderViolation(object invalidProvider)
        {
            try
            {
                _ = await owner.InvokeAsync(caller => Query(caller, invalidProvider));
                throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
            }
            catch (InvalidOperationException failure) when (failure.Message != AllocatorQueryMailboxHostNames.Failure)
            {
                Require(!context.Cancellation.IsCancellationRequested);
            }
        }
    }

    private static async Task MalformedAndZeroCount()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await owner.Ready;
        Term marker = Term.A(AllocatorQueryMailboxHostNames.Marker);
        object malformed = Provider(1, (
            caller,
            _,
            _,
            post,
            _
        ) =>
        {
            post(Nil.Value, Term.I(7));
            caller.Mailbox.Send(marker);

            return Task.CompletedTask;
        });
        try
        {
            _ = await owner.InvokeAsync(caller => Query(caller, malformed));
            throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
        {
            Require(context.Mailbox.Count == 1);
        }
        Require((await owner.InvokeAsync(async caller => await caller.ReceiveAsync(value => value, TimeSpan.Zero)))!.Equals(marker));
        Term? leftover = null;
        object zero = Provider(
            0,
            (
                caller,
                reference,
                _,
                _,
                _
            ) =>
        {
            leftover = Term.Tuple(reference, Nil.Value, Nil.Value);
            caller.Mailbox.Send(leftover);

            return Task.CompletedTask;
        }
        );
        Require((await owner.InvokeAsync(caller => Query(caller, zero))).Equals(Nil.Value));
        Require((await owner.InvokeAsync(async caller => await caller.ReceiveAsync(value => value, TimeSpan.Zero)))!.Equals(leftover));
    }

    private static object Provider(
        int replies,
        Func<ProcessContext, ReferenceTerm, Term, Action<Term, Term>, CancellationToken, Task> producer,
        CancellationToken retirement = default
    ) => typeof(ProcessRuntime).Assembly.GetType(AllocatorQueryMailboxHostNames.ProviderType)!
        .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single()
        .Invoke([new HashSet<string>(StringComparer.Ordinal) { AllocatorQueryMailboxHostNames.First }, replies, producer, retirement]);

    private static async Task<Term> Query(ProcessContext context, object provider, Term? input = null)
    {
        object pending = typeof(ProcessRuntime).Assembly.GetType(AllocatorQueryMailboxHostNames.MailboxType)!
            .GetMethod(AllocatorQueryMailboxHostNames.Gather, BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [context, input ?? Term.A(AllocatorQueryMailboxHostNames.First), provider])!;

        return await (ValueTask<Term>)pending;
    }

    private static async ValueTask<Term?> ReceiveOperation(
        ProcessContext context,
        Func<Term, Term?> select,
        CancellationToken cancellation
    )
    {
        object pending = typeof(ProcessContext).GetMethod(
            AllocatorQueryMailboxHostNames.OperationReceive, BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(typeof(Term)).Invoke(context, [select, cancellation])!;

        return await (ValueTask<Term?>)pending;
    }

    private static async Task Waiting(ProcessContext context)
    {
        FieldInfo field = typeof(ProcessContext).GetField(
            AllocatorQueryMailboxHostNames.WaitingField, BindingFlags.NonPublic | BindingFlags.Instance)!;
        var watch = Stopwatch.StartNew();
        while (!(bool)field.GetValue(context)!)
        {
            Require(watch.Elapsed < AllocatorQueryMailboxHostNames.Deadline);
            await Task.Yield();
        }
    }

    private static async Task Cancelled(Task pending)
    {
        try
        {
            await pending.WaitAsync(AllocatorQueryMailboxHostNames.Deadline);
            throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static Term Information(int identifier, Term payload) => Term.List(Term.Tuple(
        Term.A(AllocatorQueryMailboxHostNames.First), Term.I(identifier), payload
    ));

    private static Term Instance(int identifier, Term payload) => Term.Tuple(
        Term.A(AllocatorQueryMailboxHostNames.Instance), Term.I(identifier), payload
    );

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(AllocatorQueryMailboxHostNames.Failure);
    }
}
