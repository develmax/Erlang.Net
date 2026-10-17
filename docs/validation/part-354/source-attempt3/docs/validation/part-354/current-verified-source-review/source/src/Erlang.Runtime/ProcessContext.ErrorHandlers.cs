namespace Erlang;

public sealed partial class ProcessContext
{
    private readonly AsyncLocal<SelectedErrorHandlerInvocation?> selectedErrorHandler = new();

    private sealed class SelectedErrorHandlerScope(
        ProcessContext context,
        SelectedErrorHandlerInvocation? previous
    ) : IDisposable
    {
        public void Dispose() => context.selectedErrorHandler.Value = previous;
    }

    internal IDisposable EnterSelectedErrorHandler(
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        var previous = selectedErrorHandler.Value;
        selectedErrorHandler.Value = new(module, function, arguments.ToArray());

        return new SelectedErrorHandlerScope(this, previous);
    }

    internal IDisposable EnterHostErrorHandler(
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        var previous = executionFrame.Value;
        executionFrame.Value = new(
            module,
            function,
            arguments.Count,
            previous,
            false,
            arguments.ToArray()
        );
        if (selectedErrorHandler.Value is { } selected)
            selected.Frame = executionFrame.Value;
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    private ErlangException CaptureUntrustedCallFailure(
        ErlangException exception,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        var frame = executionFrame.Value;
        var selected = selectedErrorHandler.Value;
        if (frame is not null && selected is not null && ReferenceEquals(frame, selected.Frame))
            return RuntimeExceptionCapture.Source(
                this,
                exception,
                frame,
                selected.Arguments,
                true
            );

        return RuntimeExceptionCapture.Call(
            this,
            exception,
            frame,
            module,
            function,
            arguments,
            false
        );
    }
}
