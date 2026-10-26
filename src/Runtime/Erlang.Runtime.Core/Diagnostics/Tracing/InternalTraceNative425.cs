namespace Erlang;

internal static class InternalTraceNative425
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || module != InternalTraceNames425.Module || !InternalTraceNames425.Signatures.Contains((function, arity))) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity) throw new InvalidOperationException(InternalTraceNames425.Foreign);
            owner.RequireBeamfileActor418(context);
            using var origin = TraceErrorOrigin427.Enter(
                context,
                module,
                function,
                arguments
            );
            try
            {
                return ValueTask.FromResult(owner.ProcessTracing.InvokeTrace425(context, function, arguments));
            }
            catch (TraceValidation425 error)
            {
                throw Capture(
                    context,
                    function,
                    arguments,
                    new ErlangException(ErlangErrorReasons.BadArgument),
                    error.Cause
                );
            }
            catch (ErlangException error)
            {
                // Preserve an already captured exception unchanged. Core validation
                // now constructs this native origin before any stack exists.
                throw context.CaptureNativeFailure(
                    error,
                    module,
                    function,
                    arguments
                );
            }
        };

        return true;
    }

    private static ErlangException Capture(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        ErlangException error,
        string? cause
    )
        => cause is null ? context.CaptureNativeFailure(
            error,
            InternalTraceNames425.Module,
            function,
            arguments
        )
            : context.CaptureNativeFailureWithInfo(
                error,
                InternalTraceNames425.Module,
                function,
                arguments,
                new MapTerm(new Dictionary<Term, Term>
                {
                    [Term.A(ProcessTraceNames.ErrorModule)] = Term.A(ProcessTraceNames.ErrorFormatter),
                    [Term.A(ProcessTraceNames.Cause)] = Term.A(cause)
                })
            );
}
