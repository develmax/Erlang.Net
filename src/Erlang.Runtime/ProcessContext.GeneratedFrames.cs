namespace Erlang;

public sealed partial class ProcessContext
{
    public IDisposable EnterGeneratedFunction(
        string module,
        string function,
        int arity,
        Term? sourceLocation,
        object continuationIdentity,
        bool tail = false
    )
    {
        var previous = executionFrame.Value;
        executionFrame.Value = new ErlangExecutionFrame(
            module,
            function,
            arity,
            tail ? previous?.Parent : previous,
            SourceLocation: sourceLocation,
            ContinuationIdentity: continuationIdentity
        );
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    internal IDisposable? EnterSourceTailCall(string module, string function)
    {
        var previous = executionFrame.Value;
        if (previous is not { SourceWrapper: true } || previous.Module != module || previous.Function != function)
            return null;
        executionFrame.Value = previous.Parent;
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }
}
