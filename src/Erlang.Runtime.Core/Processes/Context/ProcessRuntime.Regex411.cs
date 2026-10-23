namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeRegexEngine411? regex411;
    private TaskCompletionSource? regex411Installing;
    private bool regex411Closing;

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
        regex411Closing = true;
        var retirement = regex411?.DisposeAsync().AsTask() ?? Task.CompletedTask;

        return regex411Installing is null ? retirement : Task.WhenAll(retirement, regex411Installing.Task);
    }

    private Task DrainRegex411()
    {
        lock (gate) return CloseRegex411();
    }

    private Task RegexProcessRetirement411(ProcessContext context, Task previous)
    {
        if (regex411 is null && regex411Installing is null) return previous;

        return Task.WhenAll(
            previous,
            regex411?.DrainProcess(context) ?? Task.CompletedTask,
            context.Self.Equals(InitializationPid) ? CloseRegex411() : Task.CompletedTask
        );
    }

    private bool RegexRetirementCompleted411 => regex411Installing is null && regex411?.RetirementCompletedSuccessfully != false;
}
