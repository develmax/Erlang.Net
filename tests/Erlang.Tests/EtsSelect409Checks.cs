using Erlang;
using static Erlang.Tests.EtsSelect409Access;
using static Erlang.Tests.EtsSelect409Names;

namespace Erlang.Tests;

internal static class EtsSelect409Checks
{
    private static Term Clause(Term pattern, Term guards, params Term[] body)
        => Term.Tuple(pattern, guards, Term.List(body));

    internal static async Task General()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var bodies = Bodies(runtime);
        EtsFoundation405Access.Require(bodies.Count == 11 && !runtime.Modules.HasExport(ModuleName, Select, 2));
        await actor.InvokeAsync(async context =>
        {
            Term table = await Call(
                bodies,
                context,
                New,
                Term.A(TableName),
                Term.List(Term.A(Ordered))
            );
            _ = await Call(
                bodies,
                context,
                Insert,
                table,
                Term.List(Term.Tuple(Term.I(3), Term.I(30)), Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)))
            );
            Term pattern = Term.Tuple(Term.A(First), Term.A(Second));
            Term program = Term.List(Clause(pattern, Term.List(Term.Tuple(Term.A(Greater), Term.A(Second), Term.I(10))), Term.A(First)));
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                table,
                program
            )).Equals(Term.List(Term.I(2), Term.I(3))));
            Term whole = Term.List(Clause(Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.A(MatchSpecNames.Whole)));
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                table,
                whole
            )).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)), Term.Tuple(Term.I(3), Term.I(30)))));
            _ = await Call(
                bodies,
                context,
                Insert,
                table,
                Term.Tuple(new FloatTerm(1.0), Term.I(11))
            );
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                table,
                whole
            )).Equals(Term.List(Term.Tuple(new FloatTerm(1.0), Term.I(11)), Term.Tuple(Term.I(2), Term.I(20)), Term.Tuple(Term.I(3), Term.I(30)))));
            Term exits = Term.List(Clause(Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.Tuple(Term.A(Divide), Term.I(1), Term.I(0))));
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                table,
                exits
            )).Equals(Term.List(Term.A(MatchSpecNames.Exit), Term.A(MatchSpecNames.Exit), Term.A(MatchSpecNames.Exit))));
            Term falseBody = Term.List(Clause(Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.A(ErlangBooleanAtoms.False)));
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                table,
                falseBody
            )).Equals(Term.List(Term.A(ErlangBooleanAtoms.False), Term.A(ErlangBooleanAtoms.False), Term.A(ErlangBooleanAtoms.False))));
            _ = await Call(
                bodies,
                context,
                Delete,
                table
            );
            Term keypos = await Call(
                bodies,
                context,
                New,
                Term.A(TableName),
                Term.List(Term.A(Ordered), Term.Tuple(Term.A(KeyPosition), Term.I(2)))
            );
            _ = await Call(
                bodies,
                context,
                Insert,
                keypos,
                Term.List(Term.Tuple(Term.I(1), Term.I(3)), Term.Tuple(Term.I(2), Term.I(1)), Term.Tuple(Term.I(3), Term.I(2)))
            );
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                keypos,
                whole
            )).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(1)), Term.Tuple(Term.I(3), Term.I(2)), Term.Tuple(Term.I(1), Term.I(3)))));
            _ = await Call(
                bodies,
                context,
                Delete,
                keypos
            );

            return true;
        });
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
            Term.A(TableName),
            Term.List(Term.A(Private))
        ));
        await actor.InvokeAsync(async context =>
        {
            EtsFoundation405Access.Require((await Call(
                bodies,
                context,
                Select,
                table,
                Nil.Value
            )).Equals(Nil.Value));
            await BadArgument(() => Call(
                bodies,
                context,
                Select,
                table,
                Term.A(BadSpec)
            ));
            await BadArgument(() => Call(
                bodies,
                context,
                Select,
                table,
                Term.List(Clause(Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.A(Unbound)))
            ));
            await BadArgument(() => Call(
                bodies,
                context,
                Select,
                Term.I(17),
                Term.A(BadSpec)
            ));

            return true;
        });
        await stranger.InvokeAsync(async context =>
        {
            await BadArgument(() => Call(
                bodies,
                context,
                Select,
                table,
                Term.A(BadSpec)
            ));

            return true;
        });
        await actor.InvokeAsync(async context => await Call(
            bodies,
            context,
            Delete,
            table
        ));
    }

    internal static async Task DetachedFunction()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var bodies = Bodies(runtime);
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm source = runtime.Functions.External(runtime, new ExternalFunctionIdentity(Term.A(RuntimeModule), Term.A(Length), 1));
            Term table = await Call(
                bodies,
                context,
                New,
                Term.A(TableName),
                Nil.Value
            );
            _ = await Call(
                bodies,
                context,
                Insert,
                table,
                Term.Tuple(Term.I(1), source)
            );
            Term spec = Term.List(Clause(Term.Tuple(Term.A(First), Term.A(Second)), Nil.Value, Term.A(Second)));
            var copied = (FunctionTerm)Cons.Items(await Call(
                bodies,
                context,
                Select,
                table,
                spec
            )).Single();
            _ = await Call(
                bodies,
                context,
                Delete,
                table
            );
            EtsFoundation405Access.Require((await copied.Invoke(context, [Term.List(Term.I(1), Term.I(2))])).Equals(Term.I(2)));
            EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));

            return true;
        });
    }
}
