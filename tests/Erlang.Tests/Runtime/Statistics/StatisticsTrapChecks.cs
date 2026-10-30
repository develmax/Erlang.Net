using static Erlang.Tests.StatisticsTrapTestAccess;

namespace Erlang.Tests;

internal static class StatisticsTrapChecks
{
    internal static async Task Authentic()
    {
        object? firstImage = null;
        foreach (bool preloaded in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime(2);
            PreparedBeamImage? original = null;
            if (preloaded)
            {
                using Stream stream = typeof(ProcessRuntime).Assembly.GetManifestResourceStream(StatisticsTrapTestNames.Resource)!;
                using var bytes = new MemoryStream();
                stream.CopyTo(bytes);
                await runtime.InitializeModulesAsync(registry =>
                {
                    original = registry.PrepareBeam(bytes.ToArray(), new BeamDependencySelection(StatisticsTrapTestNames.ImageDependencies));
                    Require(registry.PublishPreparedBeam(original).Status == BeamPublicationStatus.Published);
                });
            }
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            var before = runtime.Modules.Exports.ToArray();
            object? previousOwner = runtime.Modules.SourceOwner(StatisticsTrapTestNames.Module, StatisticsTrapTestNames.Helper, 2);
            await actor.InvokeAsync(async context =>
            {
                if (!preloaded)
                {
                    object? preparedBefore = Read(runtime.Modules, "statisticsTrapPreparation");
                    object? imageBefore = Read(runtime.Modules, "statisticsTrapPrivateImage");
                    Require(preparedBefore is not null && imageBefore is not null);
                    object referenceGate = Read(runtime, "referenceGate")!;
                    long beforeReference;
                    lock (referenceGate)
                        beforeReference = (long)Read(runtime, "nextRef")!;
                    Term noOp = await runtime.Modules.Call(
                        context,
                        "erlang",
                        "system_flag",
                        Term.A("microstate_accounting"),
                        Term.A("false")
                    );
                    Require(noOp.Equals(Term.A("false")));
                    lock (referenceGate)
                        Require((long)Read(runtime, "nextRef")! == beforeReference);
                    Require(ReferenceEquals(preparedBefore, Read(runtime.Modules, "statisticsTrapPreparation"))
                        && ReferenceEquals(imageBefore, Read(runtime.Modules, "statisticsTrapPrivateImage")));
                }
                object admission = await Admission(context);
                try
                {
                    object image = Read(admission, "Image")!;
                    Require((string)Read(image, "OriginalBeamSha256")! == StatisticsTrapTestNames.ImageHash);
                    Require(ReferenceEquals(Read(image, "Owner"), runtime.Modules));
                    if (preloaded)
                        Require(ReferenceEquals(image, original) && !ReferenceEquals(image, firstImage)
                            && !ReferenceEquals(image, Read(runtime.Modules, "statisticsTrapPrivateImage")));
                    else
                    {
                        firstImage = image;
                        object second = await Admission(context);
                        try
                        {
                            Require(ReferenceEquals(image, Read(second, "Image")));
                        }
                        finally
                        {
                            Call(second, "Dispose");
                        }
                        Require(previousOwner is null);
                    }
                    bool refused = false;
                    try
                    {
                        object wrong = Call(
                            runtime.Modules,
                            "AdmitStatisticsTrap",
                            context,
                            "trace",
                            3
                        )!;
                        Task wrongTask = (Task)wrong.GetType().GetMethod("AsTask")!.Invoke(wrong, null)!;
                        await wrongTask;
                    }
                    catch (InvalidOperationException)
                    {
                        refused = true;
                    }
                    Require(refused);
                }
                finally
                {
                    Call(admission, "Dispose");
                }

                return Nil.Value;
            });
            Require(before.SequenceEqual(runtime.Modules.Exports));
            Require(ReferenceEquals(previousOwner, runtime.Modules.SourceOwner(StatisticsTrapTestNames.Module, StatisticsTrapTestNames.Helper, 2)));
        }
        await using var firstRuntime = new ProcessRuntime(2);
        await using var secondRuntime = new ProcessRuntime(2);
        await using var firstActor = new RuntimeNodeFoundationHostActor(firstRuntime);
        await using var secondActor = new RuntimeNodeFoundationHostActor(secondRuntime);
        object firstAdmission = await firstActor.InvokeAsync(Admission);
        try
        {
            await secondActor.InvokeAsync(async context =>
            {
                bool refused = false;
                try
                {
                    await (ValueTask<Term>)Call(
                        secondRuntime.Modules,
                        "InvokeStatisticsTrap",
                        context,
                        firstAdmission,
                        StatisticsTrapTestNames.Helper,
                        new Term[] { secondRuntime.MakeReference(), Term.A("true") }
                    )!;
                }
                catch (InvalidOperationException)
                {
                    refused = true;
                }
                Require(refused);

                return Nil.Value;
            });
        }
        finally
        {
            Call(firstAdmission, "Dispose");
        }
    }

    internal static async Task Replies()
    {
        await using var operation = new StatisticsTrapTestOperation();
        await operation.Start(false);
        Require(!operation.Invocation!.IsCompleted);
        operation.Release.Set();
        Require((await operation.Invocation.WaitAsync(StatisticsTrapTestNames.Deadline)).Equals(Term.A("true")));
        operation.AssertNoRequestReplies();
        Require(!operation.Context.Cancellation.IsCancellationRequested);
    }

    internal static async Task Fault()
    {
        await using var operation = new StatisticsTrapTestOperation();
        await operation.Start(true);
        // Actual source receive is waiting before the actual normal callback fails.
        Require((bool)Read(operation.Context, "WaitingForMessage")!);
        operation.AllowFault.Set();
        await operation.ActualFailure.WaitAsync(StatisticsTrapTestNames.Deadline);
        await WaitUntil(() => !(bool)Read(operation.Context, "WaitingForMessage")!);
        Require(!operation.Invocation!.IsCompleted && !operation.Context.Cancellation.IsCancellationRequested);
        operation.Release.Set();
        Exception actual = await Failed(operation.Invocation, StatisticsTrapTestNames.CallbackFailure);
        Require(ReferenceEquals(actual, operation.CallbackError));
        operation.AssertNoRequestReplies();
        Require(!(bool)Read(operation.Context, "Terminated")! && !operation.Context.Cancellation.IsCancellationRequested);
        Term? foreign = await operation.Actor.InvokeAsync(async context =>
            await context.Mailbox.ReceiveAsync<Term>(value => value.Equals(operation.Foreign) ? value : null,
                TimeSpan.Zero, context.Cancellation));
        Require(foreign is not null && foreign.Equals(operation.Foreign));
        await CheckFaultIdentity(
            new InvalidOperationException(StatisticsTrapTestNames.CallbackFailure),
            new InvalidOperationException(StatisticsTrapTestNames.IndependentFailure)
        );
        var suppliedAggregate = new AggregateException(
            new InvalidOperationException(StatisticsTrapTestNames.CallbackFailure),
            new InvalidOperationException(StatisticsTrapTestNames.IndependentFailure)
        );
        await CheckFaultIdentity(suppliedAggregate, null);
    }

    private static async Task CheckFaultIdentity(Exception callback, Exception? held)
    {
        await using var operation = new StatisticsTrapTestOperation(callback, held);
        await operation.Start(true);
        operation.AllowFault.Set();
        await operation.ActualFailure.WaitAsync(StatisticsTrapTestNames.Deadline);
        await WaitUntil(() => !(bool)Read(operation.Context, "WaitingForMessage")!);
        Require(!operation.Invocation!.IsCompleted && !operation.Context.Cancellation.IsCancellationRequested);
        operation.Release.Set();
        Exception actual = await Failed(operation.Invocation);
        if (held is null)
            Require(ReferenceEquals(actual, callback));
        else
        {
            Require(actual is AggregateException aggregate && aggregate.InnerExceptions.Count == 2
                && aggregate.InnerExceptions.Any(error => ReferenceEquals(error, callback))
                && aggregate.InnerExceptions.Any(error => ReferenceEquals(error, held)));
        }
        operation.AssertNoRequestReplies();
        Require(!(bool)Read(operation.Context, "Terminated")! && !operation.Context.Cancellation.IsCancellationRequested);
    }

    internal static async Task Cancel()
    {
        await using var operation = new StatisticsTrapTestOperation();
        await operation.Start(false);
        await operation.Killer.InvokeAsync(context =>
        {
            operation.Runtime.Exit(context, operation.Actor.Process.Pid, Term.A("kill"));

            return Task.FromResult<Term>(Nil.Value);
        });
        Require(!operation.Actor.Process.Completion.IsCompleted && !operation.Invocation!.IsCompleted);
        operation.Release.Set();
        await Failed(operation.Invocation!);
        await operation.Actor.Process.Completion.WaitAsync(StatisticsTrapTestNames.Deadline);
        Require(((IReadOnlyList<Term>)Call(operation.Context.Mailbox, "Snapshot")!).Count == 0);
    }

    internal static async Task Close()
    {
        await using var operation = new StatisticsTrapTestOperation();
        await operation.Start(false);
        Task closing = operation.Runtime.DisposeAsync().AsTask();
        Require(!closing.IsCompleted);
        Require((int)Read(operation.Runtime.Modules, "statisticsTrapUses")! > 0);
        operation.Release.Set();
        await closing.WaitAsync(StatisticsTrapTestNames.Deadline);
        await Failed(operation.Invocation!);
        Require((int)Read(operation.Runtime.Modules, "statisticsTrapUses")! == 0);
    }
}
