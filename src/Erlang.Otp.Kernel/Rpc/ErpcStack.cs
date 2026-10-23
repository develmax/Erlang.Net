namespace Erlang.Otp;

internal static class ErpcStack
{
    internal static Term Trim(
        Term stack,
        Term module,
        Term function,
        Term arguments
    )
    {
        Term[] frames = Cons.Items(stack).ToArray();
        int cut = Array.FindIndex(frames, IsCut);
        if (cut < 0)
            return stack;
        if (cut == 0)
            return Term.List(Term.Tuple(
                module,
                function,
                arguments,
                Nil.Value
            ));
        if (cut == 1 && frames[0] is TupleTerm { Items: [var m, var f, var a, _] } &&
            m.Equals(module) && f.Equals(function) && a.Equals(arguments))
            return Term.List(frames[0]);
        int count;
        try
        {
            count = Cons.Items(arguments).Count();
        }
        catch (ErlangException)
        {
            return Cons.From(frames.Take(cut));
        }
        if (frames[cut - 1] is TupleTerm { Items: [var targetModule, var targetFunction, Integer arity, _] } &&
            targetModule.Equals(module) && targetFunction.Equals(function) && arity.Value == count)
            return Cons.From(frames.Take(cut));

        return Cons.From(frames.Take(cut), Term.List(Term.Tuple(
            module,
            function,
            Term.I(count),
            Nil.Value
        )));
    }

    private static bool IsCut(Term frame) => frame is TupleTerm { Items.Count: >= 2 } tuple &&
        tuple.Items[0].Equals(ErpcAtoms.Module) && tuple.Items[1] is Atom { Name: ErpcNames.Execute or ErpcNames.Cast };
}
