using System.Collections;
using System.Reflection;
using static Erlang.Tests.InitializerClose615Names;

namespace Erlang.Tests;

internal static class InitializerClose615Checks
{
    // The existing dictionary gate pins ACTUAL Dispose/Terminate between
    // mailbox admission-stop and Stop.Cancel. No lifecycle fields are mutated.
    internal static async Task ClosedBeforeCancellation()
    {
        var options = new ProcessRuntimeOptions();
        typeof(ProcessRuntimeOptions).GetProperty(OwnedGuest506Names.PrivateOption, Members)!.SetValue(options, true);
        var runtime = new ProcessRuntime(options);
        RuntimeNodeFoundationHostActor? killer = null;
        Task? disposal = null;
        try
        {
            await runtime.InitializeModulesAsync(_ => { }).AsTask().WaitAsync(Deadline);
            object initializer = Field(runtime, Initializer);
            object initializerGate = Field(initializer, Gate);
            Task completion = (Task)initializer.GetType().GetProperty(Completion, Members)!.GetValue(initializer)!;
            ProcessContext context;
            lock (Field(runtime, Gate))
                context = (ProcessContext)((IDictionary)Field(runtime, Processes))[runtime.InitializationPid]!;
            killer = new(runtime);
            await killer.Ready.WaitAsync(Deadline);
            lock (Field(context.Dictionary, Gate))
            {
                // A real exit avoids Dispose's separate dispatcher task that
                // may cancel the initializer before Terminate reaches this seam.
                disposal = killer.Invoke(sender =>
                {
                    runtime.Exit(sender, runtime.InitializationPid, Term.A(Kill));

                    return true;
                });
                // The actual mailbox drainer wakes Receive; Run's finally marks
                // stopped before InitializationEnded waits for the runtime gate.
                Require(SpinWait.SpinUntil(() =>
                {
                    lock (initializerGate)
                        return (bool)Field(initializer, Stopped);
                }, Deadline));
                Require((bool)Field(context, Terminated));
                Require((bool)Field(context.Mailbox, Closed));
                Console.WriteLine(
                    StateObservation,
                    context.Cancellation.IsCancellationRequested,
                    completion.Status,
                    disposal.Status
                );
                Require(!context.Cancellation.IsCancellationRequested);
                Require(!completion.IsCompleted);
                Require(!disposal.IsCompleted);
            }
            await disposal.WaitAsync(Deadline);
            await completion.WaitAsync(Deadline);
            await ((TaskCompletionSource<Term>)Property(context, Done)).Task.WaitAsync(Deadline);
            Require(!runtime.IsAlive(runtime.InitializationPid));
        }
        finally
        {
            if (disposal is not null)
                await disposal.WaitAsync(Deadline);
            await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            if (killer is not null)
                await killer.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
    }

    internal static async Task LiveCancellationRemainsVisible()
    {
        using var writer = new CancellationWriter();
        var runtime = new ProcessRuntime(output: writer);
        bool disposed = false;
        try
        {
            await runtime.InitializeModulesAsync(_ => { }).AsTask().WaitAsync(Deadline);
            object initializer = Field(runtime, Initializer);
            Task completion = (Task)initializer.GetType().GetProperty(Completion, Members)!.GetValue(initializer)!;
            ProcessContext context;
            lock (Field(runtime, Gate))
                context = (ProcessContext)((IDictionary)Field(runtime, Processes))[runtime.InitializationPid]!;
            writer.Context = context;
            runtime.Send(
                runtime.InitializationPid,
                Term.Tuple(
                    Term.A(Request),
                    runtime.InitializationPid,
                    Term.I(1),
                    Term.Tuple(Term.A(PutCharacters), Term.A(Unicode), Cons.From([Term.I('x')]))
                )
            );
            await writer.Entered.Task.WaitAsync(Deadline);
            Require(writer.WasLive && writer.Uncancelled);
            await RequireCancellation(completion);
            await RequireCancellation(runtime.DisposeAsync().AsTask());
            disposed = true;
            await ((TaskCompletionSource<Term>)Property(context, Done)).Task.WaitAsync(Deadline);
        }
        finally
        {
            if (!disposed)
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (OperationCanceledException error) when (error.Message == Sentinel) { }
        }
    }

    private static async Task RequireCancellation(Task operation)
    {
        bool observed = false;
        try
        {
            await operation.WaitAsync(Deadline);
        }
        catch (OperationCanceledException error) when (error.Message == Sentinel)
        {
            observed = true;
        }
        Require(observed);
    }

    private static object Property(object owner, string name)
        => owner.GetType().GetProperty(name, Members)!.GetValue(owner)!;

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private sealed class CancellationWriter : StringWriter
    {
        internal ProcessContext? Context;
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool WasLive, Uncancelled;

        public override Task WriteAsync(string? value)
        {
            WasLive = !(bool)Field(Context!, Terminated);
            Uncancelled = !Context!.Cancellation.IsCancellationRequested;
            Entered.TrySetResult();

            return Task.FromException(new OperationCanceledException(Sentinel));
        }
    }
}
