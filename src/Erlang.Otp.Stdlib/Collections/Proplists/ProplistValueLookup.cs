namespace Erlang;

internal static class ProplistValueLookup
{
    // OTP proplists:get_value/3 examines only the reached prefix. A matching
    // tuple with an arity other than two shadows subsequent properties.
    internal static async ValueTask<Term> ReadAsync(
        ProcessContext context,
        Term key,
        Term properties,
        Term defaultValue
    )
    {
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            Term property = cell.Head;
            if (property is Atom atom && atom.Equals(key))
                return ProplistPropertyAtoms.ShorthandValue;

            if (property is TupleTerm tuple
                && tuple.Items.Count != 0
                && tuple.Items[0].Equals(key))
            {
                return tuple.Items.Count == 2 ? tuple.Items[1] : defaultValue;
            }

            properties = cell.Tail;
        }

        if (properties is not Nil)
        {
            var frame = context.CaptureExecutionChain();
            if (frame is { SourceWrapper: true }
                && frame.Module == ProplistModuleNames.Module
                && frame.Function == ProplistModuleNames.GetValue)
            {
                frame = frame.Parent;
            }

            throw RuntimeExceptionCapture.Call(
                context,
                new ErlangException(ErlangErrorReasons.FunctionClause),
                frame,
                ProplistModuleNames.Module,
                ProplistModuleNames.GetValue,
                [key, properties, defaultValue],
                false,
                ProplistSourceLocation.GetValue
            );
        }

        return defaultValue;
    }
}
