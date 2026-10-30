using static Erlang.Tests.StatisticsTrapTestAccess;

namespace Erlang.Tests;

internal sealed class StatisticsTrapTestOperation : IAsyncDisposable
{
    internal ProcessRuntime Runtime { get; } = new(2);
    internal RuntimeNodeFoundationHostActor Actor { get; }
    internal RuntimeNodeFoundationHostActor Killer { get; }
    internal readonly ManualResetEventSlim Release = new();
    internal readonly ManualResetEventSlim AllowFault = new();
    internal readonly TaskCompletionSource HeldCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource Setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal ProcessContext Context { get; private set; } = null!;
    internal ReferenceTerm Reference { get; private set; } = null!;
    internal Term Foreign { get; private set; } = null!;
    internal object Request { get; private set; } = null!;
    internal Task<Term>? Invocation { get; private set; }

    internal Exception CallbackError { get; }
    internal Exception? HeldCallbackError { get; }

    internal StatisticsTrapTestOperation(Exception? callbackError = null, Exception? heldCallbackError = null)
    {
        CallbackError = callbackError ?? new InvalidOperationException(StatisticsTrapTestNames.CallbackFailure);
        HeldCallbackError = heldCallbackError;
        Actor = new(Runtime);
        Killer = new(Runtime);
    }

    internal async Task Start(bool fault)
    {
        await Killer.InvokeAsync(async context => await Runtime.Modules.Call(
            context,
            "erlang",
            "process_flag",
            Term.A("scheduler"),
            Term.I(2)
        ));
        await Actor.InvokeAsync(async context =>
        {
            Context = context;

            return await Runtime.Modules.Call(
                context,
                "erlang",
                "process_flag",
                Term.A("scheduler"),
                Term.I(2)
            );
        });
        Reference = Runtime.MakeReference();
        Foreign = Term.Tuple(Runtime.MakeReference(), Term.A(StatisticsTrapTestNames.ForeignMessage));
        if (!fault)
            AllowFault.Set();
        Invocation = Actor.InvokeAsync(async context =>
        {
            object? admission = null;
            try
            {
                admission = await Admission(context);
                Runtime.Send(context.Self, Foreign);
                Request = StatisticsTrapTestAccess.Request(Runtime, context, Reference);
                object dispatcher = Read(Runtime, "actorDispatcher")!;
                Action<int> callback = identity =>
                {
                    if (identity == 1)
                    {
                        HeldCallback.TrySetResult();
                        Require(Release.Wait(StatisticsTrapTestNames.Deadline));
                        if (HeldCallbackError is { } heldFailure)
                            throw heldFailure;
                    }
                    else if (fault)
                    {
                        Require(AllowFault.Wait(StatisticsTrapTestNames.Deadline));
                        throw CallbackError;
                    }
                    Call(Request, "Reply", new object?[] { null });
                };
                object batch = Call(dispatcher, "PrepareAuxiliary420", callback)!;
                Call(Request, "Observe", batch);
                Task completion = (Task)Read(batch, "Completion")!;
                Call(batch, "Publish");
                Setup.TrySetResult();

                return await (ValueTask<Term>)Call(
                    Request,
                    "RunHelper",
                    admission,
                    StatisticsTrapTestNames.Helper,
                    completion,
                    new Term[] { Reference, Term.A("true") }
                )!;
            }
            catch (Exception error)
            {
                Setup.TrySetException(error);
                throw;
            }
            finally
            {
                if (admission is not null)
                    Call(admission, "Dispose");
            }
        });
        await Setup.Task.WaitAsync(StatisticsTrapTestNames.Deadline);
        await HeldCallback.Task.WaitAsync(StatisticsTrapTestNames.Deadline);
        await WaitUntil(() => (bool)Read(Context, "WaitingForMessage")!);
    }

    internal Task ActualFailure => (Task)Read(Read(Request, "ReceiveFailure")!, "Failure")!;

    internal void AssertNoRequestReplies()
    {
        var values = (IReadOnlyList<Term>)Call(Context.Mailbox, "Snapshot")!;
        Require(values.Contains(Foreign));
        Require(!values.Any(value => value.Equals(Reference)
            || value is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(Reference)));
    }

    public async ValueTask DisposeAsync()
    {
        Release.Set();
        AllowFault.Set();
        if (Invocation is not null)
            try
            {
                await Invocation.WaitAsync(StatisticsTrapTestNames.Deadline);
            }
            catch (Exception) when (Invocation.IsCompleted)
            {
                // Expected injected errors are asserted by the actual test body.
            }
        await Actor.DisposeAsync();
        await Killer.DisposeAsync();
        await Runtime.DisposeAsync();
        Release.Dispose();
        AllowFault.Dispose();
    }
}
