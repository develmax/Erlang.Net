namespace Erlang;

internal static class ProcessInfoFrames
{
    public static Term Current(ErlangExecutionFrame? frame) => frame is null ? ProcessInfoAtoms.Undefined : Term.Tuple(
        Term.A(frame.Module),
        Term.A(frame.Function),
        Term.I(frame.Arity)
    );

    public static Term Stack(ErlangExecutionFrame? frame, int depth)
    {
        var frames = new List<Term>();
        object? previousContinuation = null;
        while (frame is not null && frames.Count < depth)
        {
            // Pinned stack sampling compares actual adjacent return PCs before
            // resolving an MFA/location. Distinct PCs may render identically.
            if (frame.ContinuationIdentity is { } continuation
                && ReferenceEquals(continuation, previousContinuation))
            {
                frame = frame.Parent;
                continue;
            }
            previousContinuation = frame.ContinuationIdentity;
            frames.Add(Term.Tuple(
                Term.A(frame.Module),
                Term.A(frame.Function),
                Term.I(frame.Arity),
                frame.SourceLocation ?? Nil.Value
            ));
            frame = frame.Parent;
        }

        return Cons.From(frames);
    }
}
