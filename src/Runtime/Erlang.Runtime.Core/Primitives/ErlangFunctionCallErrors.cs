namespace Erlang;

internal static class ErlangFunctionCallErrors
{
    private const int MinimumCapturedFrames = 1;

    // A non-tail fun application retains its suspended call site. A tail
    // application has no additional continuation to record on admission failure.
    internal static ErlangException Capture(ProcessContext context, Term reason, bool includeContinuation = true)
    {
        ErlangException captured = context.CaptureSourceFailure(new ErlangException(reason));
        if (!includeContinuation)
        {
            return captured;
        }
        Term stack = captured.StackTraceTerm;
        if (stack is not Cons first)
        {
            return captured;
        }
        Term repeated = Cons.From(new[] { first.Head }.Concat(Cons.Items(stack)).Take(Math.Max(MinimumCapturedFrames, context.Runtime.BacktraceDepth)));

        return new ErlangException(reason).WithCapturedStack(repeated);
    }
}
