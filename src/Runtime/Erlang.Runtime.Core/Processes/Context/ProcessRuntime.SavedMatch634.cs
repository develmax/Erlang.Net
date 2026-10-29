namespace Erlang;

public sealed partial class ProcessRuntime
{
    private HashSet<MatchSavedProgram634>? savedMatchPrograms634;
    private List<Exception>? savedMatchFailures634;
    private bool savedMatchClosing634;
    private Task? savedMatchShutdown634;

    internal void RequireSavedMatchActor634(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(savedMatchClosing634, this);
        }
    }

    internal void PublishSavedMatch634(ProcessContext context, MatchSavedProgram634 program)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(savedMatchClosing634, this);
            if (!program.OwnedBy634(this))
                throw new ArgumentException(MatchSavedNames634.Foreign, nameof(program));
            if (!(savedMatchPrograms634 ??= new()).Add(program))
                throw new InvalidOperationException(MatchSavedNames634.Invalid);
        }
    }

    internal MatchSavedReader634 ReadSavedMatch634(MatchSavedProgram634 program)
    {
        lock (gate)
        {
            if (!program.OwnedBy634(this))
                throw new ArgumentException(MatchSavedNames634.Foreign, nameof(program));
            ObjectDisposedException.ThrowIf(savedMatchClosing634 || disposed || IsHaltRequested, this);
            if (savedMatchPrograms634?.Contains(program) != true)
                throw new ObjectDisposedException(MatchSavedNames634.Closed);

            return program.AcquireRegistered634();
        }
    }

    internal bool SavedMatchAdmissionClosed634
    {
        get
        {
            lock (gate)
                return savedMatchClosing634;
        }
    }

    internal void ForgetSavedMatch634(MatchSavedProgram634 program, Exception? failure)
    {
        lock (gate)
        {
            savedMatchPrograms634?.Remove(program);
            if (failure is not null)
                (savedMatchFailures634 ??= new()).Add(failure);
        }
    }

    private Task CloseSavedMatchPrograms634()
    {
        lock (gate)
        {
            savedMatchClosing634 = true;
            if (savedMatchShutdown634 is not null)
                return savedMatchShutdown634;
            if ((savedMatchPrograms634?.Count ?? 0) == 0)
                return savedMatchShutdown634 = (savedMatchFailures634?.Count ?? 0) == 0
                    ? Task.CompletedTask
                    : Task.FromException(new AggregateException(MatchSavedNames634.Cleanup, savedMatchFailures634!));
            if (ExecutionContext.IsFlowSuppressed())
                return savedMatchShutdown634 = Task.Run(CloseSavedMatchesOutside634);
            using (ExecutionContext.SuppressFlow())
                return savedMatchShutdown634 = Task.Run(CloseSavedMatchesOutside634);
        }
    }

    private async Task CloseSavedMatchesOutside634()
    {
        MatchSavedProgram634[] snapshot;
        List<Exception> errors;
        lock (gate)
        {
            snapshot = savedMatchPrograms634?.ToArray() ?? [];
            errors = savedMatchFailures634 is null ? new() : new(savedMatchFailures634);
        }
        var tasks = new List<Task>();
        foreach (var program in snapshot)
        {
            try
            {
                tasks.Add(program.Close634());
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        Task all = Task.WhenAll(tasks);
        try
        {
            await all.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(all.Exception ?? error);
        }
        if (errors.Count != 0)
            throw new AggregateException(MatchSavedNames634.Cleanup, errors);
    }

    private bool SavedMatchRetirementCompleted634
        => (savedMatchPrograms634?.Count ?? 0) == 0 && (savedMatchFailures634?.Count ?? 0) == 0 && (savedMatchShutdown634 is null || savedMatchShutdown634.IsCompletedSuccessfully);
}
