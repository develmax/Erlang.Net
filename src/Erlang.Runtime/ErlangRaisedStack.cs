namespace Erlang;

internal static class ErlangRaisedStack
{
    public static Term Raise(IReadOnlyList<Term> arguments, int maximumDepth)
    {
        if (arguments[0] is not Atom { Name: ErlangExceptionClasses.Error or ErlangExceptionClasses.Exit or ErlangExceptionClasses.Throw } exceptionClass)
            return Term.A(ErlangErrorReasons.BadArgument);
        var frames = new List<Term>();
        Term remaining = arguments[2];
        while (remaining is Cons cell)
        {
            Term? frame = Normalize(cell.Head);
            if (frame is null)
                return Term.A(ErlangErrorReasons.BadArgument);
            if (frames.Count < Math.Max(1, maximumDepth))
                frames.Add(frame);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            return Term.A(ErlangErrorReasons.BadArgument);
        throw new ErlangException(arguments[1], exceptionClass.Name)
        {
            StackTraceTerm = Cons.From(frames),
            ExplicitStackTrace = true
        };
    }

    private static Term? Normalize(Term value)
    {
        if (value is not TupleTerm tuple)
            return null;
        var fields = tuple.Items;
        if (fields.Count == 2 && fields[0] is FunctionTerm)
            return Term.Tuple(fields[0], fields[1], Nil.Value);
        if (fields.Count == 3 && fields[0] is Atom && fields[1] is Atom)
            return Term.Tuple(
                fields[0],
                fields[1],
                fields[2],
                Nil.Value
            );
        if (fields.Count == 3 && fields[0] is FunctionTerm && fields[2] is Cons or Nil || fields.Count == 4 && fields[0] is Atom && fields[1] is Atom && fields[3] is Cons or Nil)
            return tuple;

        return null;
    }
}
