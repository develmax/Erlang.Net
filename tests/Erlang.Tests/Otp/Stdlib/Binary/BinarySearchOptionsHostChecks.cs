namespace Erlang.Tests;

internal static class BinarySearchOptionsHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            BitString subject = new([97, 98, 97, 98, 97]);
            Term patterns = Term.List(new BitString([97]), new BitString([97, 98]), new BitString([98, 97]));
            Term[] expected = [Term.Tuple(Term.I(0), Term.I(2)), Term.Tuple(Term.I(2), Term.I(2)), Term.Tuple(Term.I(4), Term.I(1))];
            Term found = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.Matches,
                subject,
                patterns
            );
            BinarySearchMfaContracts.Require(found.Equals(Cons.From(expected)));
            Term scope = Term.Tuple(Term.A(BinarySearchHostNames.Scope), Term.Tuple(Term.I(5), Term.I(-3)));
            Term options = new Cons(scope, Term.A(BinarySearchHostNames.Tail));
            found = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.Match,
                subject,
                patterns,
                options
            );
            BinarySearchMfaContracts.Require(found.Equals(Term.Tuple(Term.I(2), Term.I(2))));
            Term zero = Term.Tuple(Term.A(BinarySearchHostNames.Scope), Term.Tuple(Term.I(0), Term.I(0)));
            found = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.Match,
                subject,
                patterns,
                Term.List(scope, zero)
            );
            BinarySearchMfaContracts.Require(found.Equals(Term.A(BinarySearchHostNames.NoMatch)));
            foreach (bool all in new[] { false, true })
            {
                Term flags = Term.List(Term.A(BinarySearchHostNames.Global), Term.A(all ? BinarySearchHostNames.TrimAll : BinarySearchHostNames.Trim));
                Term result = await runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Split,
                    new BitString([97, 98, 97]),
                    new BitString([97]),
                    flags
                );
                BinarySearchMfaContracts.Require(result.Equals(all ? Term.List(new BitString([98])) : Term.List(new BitString([]), new BitString([98]))));
            }
            await BinarySearchMfaContracts.Invalid(context, BinaryModuleNames.Match, [subject, patterns, Term.List(Term.A(BinarySearchHostNames.Global))]);
            await BinarySearchMfaContracts.Invalid(context, BinaryModuleNames.Match, [subject, new BitString([])]);
            await BinarySearchMfaContracts.Invalid(context, BinaryModuleNames.Match, [subject, new Cons(new BitString([97]), Term.A(BinarySearchHostNames.Tail))]);

            return Nil.Value;
        });
    }
}
