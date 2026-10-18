namespace Erlang.Tests;

internal static class OrdsetFullMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == OrdsetFullHostNames.Module && (function, arity) is
            (OrdsetFullHostNames.FromList, 1)
            or (OrdsetFullHostNames.DeleteElement, 2)
            or (OrdsetFullHostNames.Union, 1)
            or (OrdsetFullHostNames.Union, 2)
            or (OrdsetFullHostNames.Intersection, 1)
            or (OrdsetFullHostNames.Intersection, 2)
            or (OrdsetFullHostNames.Disjoint, 2)
            or (OrdsetFullHostNames.Subtract, 2)
            or (OrdsetFullHostNames.Subset, 2)
            or (OrdsetFullHostNames.Fold, 3)
            or (OrdsetFullHostNames.Filter, 2)
            or (OrdsetFullHostNames.Map, 2)
            or (OrdsetFullHostNames.FilterMap, 2);

    internal static async Task Run(string function, int arity)
    {
        if (!Owns(OrdsetFullHostNames.Module, function, arity))
            throw new InvalidOperationException(OrdsetFullHostNames.Failure);
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term three = Term.I(3);
            Term set = Term.List(one, two, three);
            var sum = new FunctionTerm(2, (_, arguments) => ValueTask.FromResult<Term>(new Integer(((Integer)arguments[0]).Value + ((Integer)arguments[1]).Value)));
            var even = new FunctionTerm(1, (_, arguments) => ValueTask.FromResult(CoreModules.Boolean(((Integer)arguments[0]).Value.IsEven)));
            var negate = new FunctionTerm(1, (_, arguments) => ValueTask.FromResult<Term>(new Integer(-((Integer)arguments[0]).Value)));
            Term[] arguments;
            Term expected;
            switch (function)
            {
                case OrdsetFullHostNames.FromList:
                    arguments = [Term.List(three, one, two, one)];
                    expected = set;
                    break;
                case OrdsetFullHostNames.DeleteElement:
                    arguments = [two, set];
                    expected = Term.List(one, three);
                    break;
                case OrdsetFullHostNames.Union when arity == 1:
                    arguments = [Term.List(Term.List(one, two), Term.List(two, three))];
                    expected = set;
                    break;
                case OrdsetFullHostNames.Union:
                    arguments = [Term.List(new FloatTerm(2.0)), Term.List(one, two)];
                    expected = Term.List(one, two);
                    break;
                case OrdsetFullHostNames.Intersection when arity == 1:
                    arguments = [Term.List(set, Term.List(two, three), Term.List(three))];
                    expected = Term.List(three);
                    break;
                case OrdsetFullHostNames.Intersection:
                    arguments = [Term.List(new FloatTerm(2.0)), Term.List(one, two)];
                    expected = Term.List(two);
                    break;
                case OrdsetFullHostNames.Disjoint:
                    arguments = [Term.List(one), Term.List(two, three)];
                    expected = CoreModules.Boolean(true);
                    break;
                case OrdsetFullHostNames.Subtract:
                    arguments = [set, Term.List(two)];
                    expected = Term.List(one, three);
                    break;
                case OrdsetFullHostNames.Subset:
                    arguments = [Term.List(new FloatTerm(2.0)), set];
                    expected = CoreModules.Boolean(true);
                    break;
                case OrdsetFullHostNames.Fold:
                    arguments = [sum, Term.I(10), set];
                    expected = Term.I(16);
                    break;
                case OrdsetFullHostNames.Filter:
                case OrdsetFullHostNames.FilterMap:
                    arguments = [even, set];
                    expected = Term.List(two);
                    break;
                default:
                    arguments = [negate, set];
                    expected = Term.List(Term.I(-3), Term.I(-2), Term.I(-1));
                    break;
            }
            Term actual = await runtime.Modules.Call(
                context,
                OrdsetFullHostNames.Module,
                function,
                arguments
            );
            if (!actual.Equals(expected))
                throw new InvalidOperationException(OrdsetFullHostNames.Failure);

            return Nil.Value;
        });
    }
}
