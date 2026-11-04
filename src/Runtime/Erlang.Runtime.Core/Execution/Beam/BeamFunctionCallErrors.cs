namespace Erlang;

internal static class BeamFunctionCallErrors
{
    private const int MinimumCapturedFrames = 1;

    // A rejected fun application and its suspended source caller are distinct
    // frames for a non-tail call. A tail call has no suspended caller frame.
    internal static ErlangException BadFunction(ProcessContext context, Term callee, bool tailPosition)
    {
        Term reason = Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), callee);
        ErlangException captured = context.CaptureSourceFailure(new ErlangException(reason));
        if (tailPosition || captured.StackTraceTerm is not Cons first)
        {
            return captured;
        }

        Term stack = Cons.From(new[] { first.Head }
            .Concat(Cons.Items(captured.StackTraceTerm))
            .Take(Math.Max(MinimumCapturedFrames, context.Runtime.BacktraceDepth)));

        return new ErlangException(reason).WithCapturedStack(stack);
    }
}
