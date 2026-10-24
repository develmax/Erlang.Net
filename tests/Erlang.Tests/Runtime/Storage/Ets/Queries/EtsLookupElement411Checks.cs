using Erlang;
using static Erlang.Tests.EtsSelect409Access;
using static Erlang.Tests.EtsLookupElement411Names;

namespace Erlang.Tests;

internal static class EtsLookupElement411Checks
{
    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var bodies = Bodies(runtime);
        EtsFoundation405Access.Require(bodies.Count == 11 && !runtime.Modules.HasExport(EtsSelect409Names.ModuleName, Function, 3));
        await actor.InvokeAsync(async context =>
        {
            foreach (Term options in new[] { Nil.Value, Term.List(Term.A(Ordered)), Term.List(Term.A(Bag)), Term.List(Term.A(DuplicateBag)) })
            {
                Term table = await Call(
                    bodies,
                    context,
                    New,
                    Term.A(Table),
                    options
                );
                _ = await Call(
                    bodies,
                    context,
                    Insert,
                    table,
                    Term.Tuple(Term.I(1), Term.I(10))
                );
                Term result = await Call(
                    bodies,
                    context,
                    Function,
                    table,
                    Term.I(1),
                    Term.I(2)
                );
                bool bag = options.Equals(Term.List(Term.A(Bag))) || options.Equals(Term.List(Term.A(DuplicateBag)));
                EtsFoundation405Access.Require(result.Equals(bag ? Term.List(Term.I(10)) : Term.I(10)));
                EtsFoundation405Access.Require((await Call(
                    bodies,
                    context,
                    Function,
                    table,
                    Term.I(1),
                    Term.I(WideSecond)
                )).Equals(result));
                Term fallback = Term.Tuple(Term.I(3), new BitString([4, 5]));
                EtsFoundation405Access.Require(ReferenceEquals(await Call(
                    bodies,
                    context,
                    Function,
                    table,
                    Term.I(7),
                    Term.I(99),
                    fallback
                ), fallback));
                _ = await Call(
                    bodies,
                    context,
                    Delete,
                    table
                );
            }
            Term ordered = await Call(
                bodies,
                context,
                New,
                Term.A(Table),
                Term.List(Term.A(Ordered), Term.Tuple(Term.A(KeyPosition), Term.I(2)))
            );
            _ = await Call(
                bodies,
                context,
                Insert,
                ordered,
                Term.Tuple(Term.I(9), Term.I(1))
            );
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Function,
                ordered,
                new FloatTerm(1.0),
                Term.I(1)
            )).Equals(Term.I(9)));
            _ = await Call(
                bodies,
                context,
                Delete,
                ordered
            );

            return true;
        });
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
    }

    internal static async Task Errors()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var stranger = new RuntimeNodeFoundationHostActor(runtime);
        var bodies = Bodies(runtime);
        Term table = await actor.InvokeAsync(async context => await Call(
            bodies,
            context,
            New,
            Term.A(Table),
            Term.List(Term.A(Private), Term.A(Bag))
        ));
        await actor.InvokeAsync(async context =>
        {
            _ = await Call(
                bodies,
                context,
                Insert,
                table,
                Term.List(Term.Tuple(Term.I(1), Term.I(2)), Term.Tuple(Term.I(1)))
            );
            await BadArgument(() => Call(
                bodies,
                context,
                Function,
                table,
                Term.I(1),
                Term.I(2),
                Term.I(99)
            ));
            await BadArgument(() => Call(
                bodies,
                context,
                Function,
                table,
                Term.I(8),
                Term.I(2)
            ));
            await BadArgument(() => Call(
                bodies,
                context,
                Function,
                table,
                Term.I(1),
                Term.I(NegativeNarrowed)
            ));
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Function,
                table,
                Term.I(8),
                Term.I(NegativeNarrowed),
                Term.I(99)
            )).Equals(Term.I(99)));
            foreach (Term position in new Term[] { Term.I(0), Term.I(-1), new FloatTerm(2.0), Term.A(Missing) })
                await BadArgument(() => Call(
                    bodies,
                    context,
                    Function,
                    table,
                    Term.I(8),
                    position,
                    Term.I(99)
                ));
            await BadArgument(() => Call(
                bodies,
                context,
                Function,
                Term.A(Missing),
                Term.I(1),
                Term.I(0)
            ));
            await BadArgument(() => Call(
                bodies,
                context,
                Function,
                Term.I(0),
                Term.I(1),
                Term.I(0)
            ));

            return true;
        });
        await stranger.InvokeAsync(async context =>
        {
            await BadArgument(() => Call(
                bodies,
                context,
                Function,
                table,
                Term.I(1),
                Term.I(0),
                Term.I(99)
            ));

            return true;
        });
        _ = await actor.InvokeAsync(async context => await Call(
            bodies,
            context,
            Delete,
            table
        ));
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
    }

    internal static async Task Resources()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var bodies = Bodies(runtime);
        BitString copied = await actor.InvokeAsync(async context =>
        {
            Term table = await Call(
                bodies,
                context,
                New,
                Term.A(Table),
                Nil.Value
            );
            var value = new BitString(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());
            _ = await Call(
                bodies,
                context,
                Insert,
                table,
                Term.Tuple(Term.I(1), value)
            );
            var result = (BitString)await Call(
                bodies,
                context,
                Function,
                table,
                Term.I(1),
                Term.I(2)
            );
            EtsFoundation405Access.Require(!ReferenceEquals(result, value) && result.Equals(value));
            _ = await Call(
                bodies,
                context,
                Delete,
                table
            );

            return result;
        });
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
        EtsFoundation405Access.Require(copied.ToArray().SequenceEqual(Enumerable.Range(0, 256).Select(i => (byte)i)));
    }

    // HOST representation boundary only: no native header-word probe or golden.
    internal static async Task Boundary()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var bodies = Bodies(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term table = await Call(
                bodies,
                context,
                New,
                Term.A(Table),
                Nil.Value
            );
            _ = await Call(
                bodies,
                context,
                Insert,
                table,
                Term.Tuple(Term.I(1), Term.I(2))
            );
            bool refused = false;
            try
            {
                _ = await Call(
                    bodies,
                    context,
                    Function,
                    table,
                    Term.I(1),
                    Term.I(ZeroProjected),
                    Term.I(99)
                );
            }
            catch (NotSupportedException error)
            {
                refused = error.Message == HeaderDiagnostic;
            }
            EtsFoundation405Access.Require(refused);
            // Missing key never reads a header; source /4 retains exact Default.
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Function,
                table,
                Term.I(8),
                Term.I(ZeroProjected),
                Term.I(99)
            )).Equals(Term.I(99)));
            _ = await Call(
                bodies,
                context,
                Delete,
                table
            );

            return true;
        });
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
    }
}
