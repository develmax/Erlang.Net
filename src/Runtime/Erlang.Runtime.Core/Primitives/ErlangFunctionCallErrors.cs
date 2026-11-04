namespace Erlang;

internal static class ErlangFunctionCallErrors
{
    private const int MinimumCapturedFrames = 1;

    // ERTS records the failed fun application and its suspended call site.
    // They can name the same MFA and line; they are two distinct frames.
    internal static ErlangException Capture(ProcessContext context, Term reason)
    {
        ErlangException captured = context.CaptureSourceFailure(new ErlangException(reason));
        Term stack = captured.StackTraceTerm;
        if (stack is not Cons first)
        {
            return captured;
        }
        Term repeated = Cons.From(new[] { first.Head }.Concat(Cons.Items(stack)).Take(Math.Max(MinimumCapturedFrames, context.Runtime.BacktraceDepth)));

        return new ErlangException(reason).WithCapturedStack(repeated);
    }
}
