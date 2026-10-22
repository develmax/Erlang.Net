namespace Erlang;

public sealed partial class ProcessContext
{
    internal IDisposable EnterBeamNifReplacement(string module, string function, int arity)
    {
        var previous = executionFrame.Value;
        if (previous is not null && previous.Module == module
            && previous.Function == function && previous.Arity == arity)
            executionFrame.Value = previous.Parent;
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    internal IDisposable EnterBeamNative(string module, string function, int arity)
    {
        var previous = executionFrame.Value;
        if (previous is { SourceWrapper: true } && previous.Module == module && previous.Function == function && previous.Arity == arity)
            executionFrame.Value = previous.Parent;
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    internal IDisposable EnterBeamFunction(string module, string function, int arity)
    {
        var previous = executionFrame.Value;
        var parent = previous is { SourceWrapper: true } && previous.Module == module ? previous.Parent : previous;
        executionFrame.Value = new(
            module,
            function,
            arity,
            parent
        );
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    internal IDisposable? EnterBeamContinuation(object identity)
    {
        var previous = executionFrame.Value;
        if (previous is null)
            return null;
        var current = previous with { ContinuationIdentity = identity };
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
}
