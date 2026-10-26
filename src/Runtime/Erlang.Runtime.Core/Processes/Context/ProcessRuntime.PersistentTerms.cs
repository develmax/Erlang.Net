namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimePersistentTerms? persistentTerms;

    internal RuntimePersistentTerms PersistentTerms
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

                return persistentTerms ??= new(this);
            }
        }
    }

    internal void RequirePersistentActor(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    internal T PublishPersistent<T>(ProcessContext context, Func<T> publish)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();

            return publish();
        }
    }

    // All runtime lifecycle close paths call under gate; joining is outside
    // the monitor and remains independent of other cleanup failures.
    private Task ClosePersistentTerms() => persistentTerms?.BeginRuntimeClose417() ?? Task.CompletedTask;

    private Task DrainPersistentTerms()
    {
        lock (gate)
            return ClosePersistentTerms();
    }

    private bool PersistentTermRetirementCompletedSuccessfully
        => persistentTerms?.RetirementCompletedSuccessfully != false;
}
