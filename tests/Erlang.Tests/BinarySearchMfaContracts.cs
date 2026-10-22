namespace Erlang.Tests;

internal static class BinarySearchMfaContracts
{
    internal static readonly (string Function, int Arity)[] Exports =
    [
        (BinaryModuleNames.CompilePattern, 1),
        (BinaryModuleNames.Match, 2), (BinaryModuleNames.Match, 3),
        (BinaryModuleNames.Matches, 2), (BinaryModuleNames.Matches, 3),
        (BinaryModuleNames.Split, 2), (BinaryModuleNames.Split, 3)
    ];

    internal static bool Owns(string module, string function, int arity)
        => module == BinaryModuleNames.Module && Exports.Contains((function, arity));

    internal static async Task Run(string function, int arity)
    {
        Require(Owns(BinaryModuleNames.Module, function, arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            BitString subject = new([97, 98, 97, 98, 97]);
            BitString pattern = new([97]);
            Term[] args = arity == 1 ? [pattern] : arity == 2 ? [subject, pattern] : [subject, pattern, Nil.Value];
            Term actual = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                function,
                args
            );
            if (arity == 1)
                Require(actual is TupleTerm { Items.Count: 2 } pair
                    && pair.Items[0].Equals(Term.A(BinarySearchHostNames.Single))
                    && pair.Items[1] is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.BinarySearchPattern });
            else if (function == BinaryModuleNames.Match)
                Require(actual.Equals(Term.Tuple(Term.I(0), Term.I(1))));
            else if (function == BinaryModuleNames.Matches)
                Require(actual.Equals(Term.List(Term.Tuple(Term.I(0), Term.I(1)), Term.Tuple(Term.I(2), Term.I(1)), Term.Tuple(Term.I(4), Term.I(1)))));
            else
                Require(actual.Equals(Term.List(new BitString([]), new BitString([98, 97, 98, 97]))));
            Term[] invalid = arity == 1 ? [Term.I(42)] : arity == 2 ? [Term.I(42), pattern] : [Term.I(42), pattern, Nil.Value];
            await Invalid(context, function, invalid);

            return Nil.Value;
        });
    }

    internal static async Task Invalid(ProcessContext context, string function, Term[] arguments)
    {
        try
        {
            await context.Runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                function,
                arguments
            );
        }
        catch (ErlangException error)
        {
            Term metadata = Term.List(Term.Tuple(
                Term.A(BinarySearchHostNames.ErrorInfo),
                new MapTerm(new Dictionary<Term, Term>
                {
                    [Term.A(BinarySearchHostNames.Module)] = Term.A(BinarySearchHostNames.Formatter)
                })
            ));
            Term expected = Term.Tuple(
                Term.A(BinaryModuleNames.Module),
                Term.A(function),
                Cons.From(arguments),
                metadata
            );
            Require(error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))
                && error.StackTraceTerm is Cons frames && frames.Head.Equals(expected));

            return;
        }
        throw new InvalidOperationException(BinarySearchHostNames.Failure);
    }

    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BinarySearchHostNames.Failure);
    }
}
