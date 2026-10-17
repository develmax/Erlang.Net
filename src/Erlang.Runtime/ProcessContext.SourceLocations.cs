namespace Erlang;

public sealed partial class ProcessContext
{
    public IDisposable? EnterSourceLocation(Term? location)
    {
        var previous = executionFrame.Value;
        if (previous is null || location is null || ReferenceEquals(previous.SourceLocation, location))
            return null;
        var current = previous with { SourceLocation = location };
        var selected = selectedErrorHandler.Value;
        if (selected is not null && ReferenceEquals(selected.Frame, previous))
            selected.Frame = current;
        else
            selected = null;
        executionFrame.Value = current;
        Volatile.Write(ref visibleExecutionFrame, current);

        return new SourceLocationScope(
            this,
            previous,
            current,
            selected
        );
    }

    private sealed class SourceLocationScope(
        ProcessContext context,
        ErlangExecutionFrame previous,
        ErlangExecutionFrame current,
        SelectedErrorHandlerInvocation? selected
    ) : IDisposable
    {
        public void Dispose()
        {
            if (selected is not null && ReferenceEquals(selected.Frame, current))
                selected.Frame = previous;
            context.executionFrame.Value = previous;
            Volatile.Write(ref context.visibleExecutionFrame, previous);
        }
    }
}
