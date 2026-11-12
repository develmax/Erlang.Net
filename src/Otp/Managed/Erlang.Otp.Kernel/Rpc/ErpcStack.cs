// Derived from OTP 29.1.1 erpc.erl, Apache-2.0. Copyright Ericsson AB 2020-2025.
namespace Erlang.Otp;

internal static class ErpcStack
{
    internal static Term Trim(
        Term stack,
        Term module,
        Term function,
        Term arguments,
        ProcessContext? context = null
    )
    {
        var frames = new List<Term>();
        Term tail = stack;
        while (tail is Cons cell)
        {
            if (IsCut(cell.Head))
            {
                if (frames.Count == 0)
                    return Term.List(Term.Tuple(
                        module,
                        function,
                        arguments,
                        Nil.Value
                    ));
                if (frames.Count == 1 && Matches(
                    frames[0],
                    module,
                    function,
                    arguments
                ))
                    return Term.List(frames[0]);
                int count;
                try
                {
                    count = Cons.Items(arguments).Count();
                }
                catch (ErlangException)
                {
                    return Cons.From(frames);
                }
                if (frames[^1] is TupleTerm { Items: [var m, var f, var arity, _] } && m.Equals(module) && f.Equals(function)
                    && (arity is Integer integer && integer.Value == count || arity is FloatTerm number && number.Value == count))
                    return Cons.From(frames);

                return Cons.From(frames.Append(Term.Tuple(
                    module,
                    function,
                    Term.I(count),
                    Nil.Value
                )));
            }
            frames.Add(cell.Head);
            tail = cell.Tail;
        }
        if (tail is Nil)
            return stack;
        if (context is null)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        var scopes = new List<IDisposable>();
        try
        {
            scopes.Add(context.EnterGeneratedFunction(
                ErpcNames.Module,
                ErpcNames.TrimStack,
                4,
                ErpcSource.Location(ErpcSourceLines.TrimEntry),
                new object()
            ));
            foreach (Term _ in frames)
                scopes.Add(context.EnterGeneratedFunction(
                    ErpcNames.Module,
                    ErpcNames.TrimAuxiliary,
                    4,
                    ErpcSource.Location(ErpcSourceLines.TrimRecursive),
                    new object()
                ));
            scopes.Add(context.EnterGeneratedFunction(
                ErpcNames.Module,
                ErpcNames.TrimAuxiliary,
                4,
                ErpcSource.Location(ErpcSourceLines.TrimClause),
                new object()
            ));

            throw new ErlangException(ErlangErrorReasons.FunctionClause).WithCapturedStack(context.CaptureErrorStack(Term.List(
                tail,
                module,
                function,
                arguments
            ), null));
        }
        finally
        {
            for (int index = scopes.Count - 1; index >= 0; index--)
                scopes[index].Dispose();
        }
    }

    private static bool Matches(
        Term frame,
        Term module,
        Term function,
        Term arguments
    )
        => frame is TupleTerm { Items: [var m, var f, var a, _] } && m.Equals(module) && f.Equals(function) && a.Equals(arguments);

    private static bool IsCut(Term frame) => frame is TupleTerm { Items.Count: >= 2 } tuple &&
        tuple.Items[0].Equals(ErpcAtoms.Module) && tuple.Items[1] is Atom { Name: ErpcNames.Execute or ErpcNames.Cast };
}
