namespace Erlang.Tests;

internal static class OrdsetCoreMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == OrdsetCoreMfaNames.Module && (function, arity) is
            (OrdsetCoreMfaNames.New, 0)
            or (OrdsetCoreMfaNames.IsSet, 1)
            or (OrdsetCoreMfaNames.Size, 1)
            or (OrdsetCoreMfaNames.IsEmpty, 1)
            or (OrdsetCoreMfaNames.ToList, 1)
            or (OrdsetCoreMfaNames.IsEqual, 2)
            or (OrdsetCoreMfaNames.IsElement, 2)
            or (OrdsetCoreMfaNames.AddElement, 2);

    internal static async Task Run(string function, int arity)
    {
        if (!Owns(OrdsetCoreMfaNames.Module, function, arity))
            throw new InvalidOperationException(OrdsetCoreMfaNames.Failure);
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term set = Term.List(one, two);
            Term invalid = Term.A(OrdsetCoreMfaNames.Invalid);
            Term tail = Term.A(OrdsetCoreMfaNames.Tail);
            Term improper = new Cons(one, tail);
            Term floatingOne = new FloatTerm(1.0);

            async ValueTask<Term> Call(params Term[] arguments)
                => await runtime.Modules.Call(
                    context,
                    OrdsetCoreMfaNames.Module,
                    function,
                    arguments
                );

            async Task Equal(Term expected, params Term[] arguments)
            {
                if (!(await Call(arguments)).Equals(expected))
                    throw new InvalidOperationException(OrdsetCoreMfaNames.Failure);
            }

            async Task Failure(string reason, params Term[] arguments)
            {
                try
                {
                    await Call(arguments);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(OrdsetCoreMfaNames.Failure);
            }
            switch (function)
            {
                case OrdsetCoreMfaNames.New:
                    await Equal(Nil.Value);
                    break;
                case OrdsetCoreMfaNames.IsEmpty:
                    await Equal(CoreModules.Boolean(true), Nil.Value);
                    await Equal(CoreModules.Boolean(false), invalid);
                    await Equal(CoreModules.Boolean(false), improper);
                    break;
                case OrdsetCoreMfaNames.ToList:
                    await Equal(invalid, invalid);
                    await Equal(improper, improper);
                    await Equal(set, set);
                    break;
                case OrdsetCoreMfaNames.IsSet:
                    await Equal(CoreModules.Boolean(true), set);
                    await Equal(CoreModules.Boolean(false), Term.List(one, floatingOne));
                    await Equal(CoreModules.Boolean(false), invalid);
                    await Failure(ErlangErrorReasons.FunctionClause, improper);
                    break;
                case OrdsetCoreMfaNames.Size:
                    await Equal(Term.I(2), set);
                    await Equal(Term.I(0), Nil.Value);
                    await Failure(ErlangErrorReasons.BadArgument, improper);
                    break;
                case OrdsetCoreMfaNames.IsEqual:
                    await Equal(CoreModules.Boolean(true), new Cons(one, two), new Cons(floatingOne, new FloatTerm(2.0)));
                    await Equal(CoreModules.Boolean(false), set, Nil.Value);
                    await Failure(ErlangErrorReasons.FunctionClause, invalid, set);
                    break;
                case OrdsetCoreMfaNames.IsElement:
                    await Equal(CoreModules.Boolean(true), floatingOne, set);
                    await Equal(CoreModules.Boolean(false), Term.I(0), improper);
                    await Failure(ErlangErrorReasons.FunctionClause, two, improper);
                    break;
                case OrdsetCoreMfaNames.AddElement:
                    await Equal(set, floatingOne, set);
                    await Equal(new Cons(Term.I(0), improper), Term.I(0), improper);
                    await Equal(Term.List(one, two, Term.I(3)), Term.I(3), set);
                    await Failure(ErlangErrorReasons.FunctionClause, two, improper);
                    break;
            }

            return Nil.Value;
        });
    }
}
