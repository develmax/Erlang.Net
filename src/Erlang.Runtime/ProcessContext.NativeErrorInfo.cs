namespace Erlang;

public sealed partial class ProcessContext
{
    internal ErlangException CaptureNativeFailureWithInfo(
        ErlangException error,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        MapTerm info
    )
        => RuntimeExceptionCapture.Call(
            this,
            error,
            executionFrame.Value,
            module,
            function,
            arguments,
            true,
            Term.List(Term.Tuple(Term.A(RuntimeExceptionNames.ErrorInfo), info))
        );
}
