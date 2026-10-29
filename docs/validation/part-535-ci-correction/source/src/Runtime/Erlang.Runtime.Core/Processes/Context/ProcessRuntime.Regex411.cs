namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeRegexEngine411? regex411;
    private TaskCompletionSource? regex411Installing;
    private bool regex411Closing;
    private Task? regexShutdown597;
    internal bool RegexShutdown597
    {
        get
        {
            lock (gate) return regex411Closing;
        }
    }

    internal RuntimeRegexEngine411? BeginRegexInstall411()
    {
        lock (gate)
        {
            RequireNativePrimitiveRegistration();
            ObjectDisposedException.ThrowIf(regex411Closing, this);
            if (regex411Installing is not null) throw new InvalidOperationException(ReDiagnosticNames411.RegexInstallationAlreadyPending);
            if (regex411 is not null) return regex411;
            regex411Installing = new(TaskCreationOptions.RunContinuationsAsynchronously);

            return null;
        }
    }

    internal void PublishRegex411(RuntimeRegexEngine411 engine)
    {
        lock (gate)
        {
            RequireNativePrimitiveRegistration();
            ObjectDisposedException.ThrowIf(regex411Closing, this);
            regex411 = engine;
        }
    }

    internal void CompleteRegexInstall411(RuntimeRegexEngine411? failed)
    {
        lock (gate)
        {
            if (failed is not null && ReferenceEquals(regex411, failed)) regex411 = null;
            regex411Installing?.TrySetResult();
            regex411Installing = null;
        }
    }

    internal void RetainFailedRegexInstallation599(RuntimeRegexEngine411 failed)
    {
        lock (gate)
        {
            if (regex411 is not null && !ReferenceEquals(regex411, failed))
                throw new InvalidOperationException(ReMagicNames597.UnpublishedDispose599);
            // A cleanup failure must not orphan its API/dispatcher participant.
            // Do not await global heap retirement from this installing actor.
            regex411 = failed;
            regex411Closing = true;
            failed.RequestClose();
            regex411Installing?.TrySetResult();
            regex411Installing = null;
        }
    }

    internal void AdmitRegex411(ProcessContext context, Action publish)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            publish();
        }
    }

    private Task CloseRegex411()
    {
        lock (gate)
        {
            if (regexShutdown597 is not null) return regexShutdown597;
            regex411Closing = true;
            regex411?.RequestClose();
            if (ExecutionContext.IsFlowSuppressed()) return regexShutdown597 = Task.Run(CloseRegexAfterRoots597);
            using (ExecutionContext.SuppressFlow()) return regexShutdown597 = Task.Run(CloseRegexAfterRoots597);
        }
    }

    private async Task CloseRegexAfterRoots597()
    {
        Task? installing;
        lock (gate) installing = regex411Installing?.Task;
        if (installing is not null) await installing.ConfigureAwait(false);
        await DrainOwnedTermContexts506().ConfigureAwait(false);
        RuntimeRegexEngine411? selected;
        lock (gate) selected = regex411;
        if (selected is not null) await selected.DisposeAsync().ConfigureAwait(false);
    }

    private Task DrainRegex411() => CloseRegex411();

    private Task RegexProcessRetirement411(ProcessContext context, Task previous)
    {
        if (regex411 is null && regex411Installing is null) return previous;
        if (context.Self.Equals(InitializationPid)) _ = CloseRegex411();

        // Global heap drain must not be awaited by a process's own Done.
        return Task.WhenAll(previous, regex411?.DrainProcess(context) ?? Task.CompletedTask);
    }

    private bool RegexRetirementCompleted411 => regex411Installing is null && regex411?.RetirementCompletedSuccessfully != false;
}
