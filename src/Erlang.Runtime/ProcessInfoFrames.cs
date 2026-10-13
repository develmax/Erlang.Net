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
        while (frame is not null && frames.Count < depth)
        {
            frames.Add(Term.Tuple(
                Term.A(frame.Module),
                Term.A(frame.Function),
                Term.I(frame.Arity),
                Nil.Value
            ));
            frame = frame.Parent;
        }

        return Cons.From(frames);
    }
}
