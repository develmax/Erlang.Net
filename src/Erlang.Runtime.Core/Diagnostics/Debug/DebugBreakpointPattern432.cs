namespace Erlang;

internal sealed record DebugBreakpointPattern432(string Module, string Function, long? Arity)
{
    internal static DebugBreakpointPattern432 Parse(Term value)
    {
        if (value is not TupleTerm { Items.Count: 3 } tuple || tuple.Items[0] is not Atom module || tuple.Items[1] is not Atom function)
            throw new ErlangException(Term.A(ErlangErrorReasons.BadArgument));
        bool m = module.Name == DebugDispatchNames432.Wildcard, f = function.Name == DebugDispatchNames432.Wildcard;
        bool a = tuple.Items[2] is Atom atom && atom.Name == DebugDispatchNames432.Wildcard;
        long? arity = null;
        if (!a)
        {
            if (tuple.Items[2] is not Integer number || number.Value < -(System.Numerics.BigInteger.One << 59) || number.Value >= (System.Numerics.BigInteger.One << 59))
                throw new ErlangException(Term.A(ErlangErrorReasons.BadArgument));
            arity = (long)number.Value;
        }
        if (m && (!f || !a) || f && !a) throw new ErlangException(Term.A(ErlangErrorReasons.BadArgument));

        return new(module.Name, function.Name, arity);
    }

    internal bool Matches(BeamLinkedFunction function)
        => (Module == DebugDispatchNames432.Wildcard || Module == function.Module)
        && (Function == DebugDispatchNames432.Wildcard || Function == function.Name)
        && (Arity is null || Arity == function.Arity);
}
