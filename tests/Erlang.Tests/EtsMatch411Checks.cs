using Erlang;
using static Erlang.Tests.EtsMatch411Access;
using static Erlang.Tests.EtsMatch411Names;

namespace Erlang.Tests;

internal static class EtsMatch411Checks
{
    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime(1);
        await Enable(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term table = await Call(context, New, Term.A(Table), Term.List(Term.A(Ordered)));
            _ = await Call(context, Insert, table, Term.List(Term.Tuple(Term.I(3), Term.I(30)), Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20))));
            Term pattern = Term.Tuple(Term.A(Second), Term.A(First));
            EtsFoundation405Access.Require((await Call(context, Function, table, pattern)).Equals(Term.List(Term.List(Term.I(10), Term.I(1)), Term.List(Term.I(20), Term.I(2)), Term.List(Term.I(30), Term.I(3)))));
            EtsFoundation405Access.Require((await Call(context, Function, table, Term.A(MatchSpecNames.Wildcard))).Equals(Term.List(Nil.Value, Nil.Value, Nil.Value)));
            _ = await Call(context, Delete, table);
            table = await Call(context, New, Term.A(Table), Nil.Value);
            _ = await Call(context, Insert, table, Term.Tuple(Term.I(10), Term.I(20), Term.I(10), Term.I(30)));
            EtsFoundation405Access.Require((await Call(context, Function, table, Term.Tuple(Term.A(Seventh), Term.A(Second), Term.A(Seventh), Term.A(Zero)))).Equals(Term.List(Term.List(Term.I(30), Term.I(20), Term.I(10)))));
            _ = await Call(context, Delete, table);
            table = await Call(context, New, Term.A(Table), Nil.Value);
            _ = await Call(context, Insert, table, Term.Tuple(Term.Tuple(Term.A(LoggerHandler), Term.I(1)), Term.I(10)));
            EtsFoundation405Access.Require((await Call(context, Function, table, Term.Tuple(Term.Tuple(Term.A(LoggerHandler), Term.A(MatchSpecNames.Wildcard)), Term.A(First)))).Equals(Term.List(Term.List(Term.I(10)))));
            _ = await Call(context, Delete, table);

            return true;
        });
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
    }

    internal static async Task Errors()
    {
        await using var runtime = new ProcessRuntime(1);
        await Enable(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var stranger = new RuntimeNodeFoundationHostActor(runtime);
        Term invalid = Term.Tuple(Term.I(1), new MapTerm([new KeyValuePair<Term, Term>(Term.A(First), Term.I(2))]));
        Term table = await actor.InvokeAsync(async context => await Call(context, New, Term.A(Table), Term.List(Term.A(Private))));
        await actor.InvokeAsync(async context =>
        {
            await BadArgument(() => Call(context, Function, table, invalid));
            await BadArgument(() => Call(context, Function, Term.A(Missing), invalid));
            await BadArgument(() => Call(context, Function, Term.I(42), invalid));

            return true;
        });
        await stranger.InvokeAsync(async context =>
        {
            await BadArgument(() => Call(context, Function, table, invalid));

            return true;
        });
        _ = await actor.InvokeAsync(async context => await Call(context, Delete, table));
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
    }

    internal static async Task Resources()
    {
        await using var runtime = new ProcessRuntime(1);
        await Enable(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm source = runtime.Functions.External(runtime, new ExternalFunctionIdentity(Term.A(Runtime), Term.A(Length), 1));
            Term table = await Call(context, New, Term.A(Table), Nil.Value);
            _ = await Call(context, Insert, table, Term.Tuple(Term.I(1), source));
            Term values = await Call(context, Function, table, Term.Tuple(Term.I(1), Term.A(First)));
            var copied = (FunctionTerm)Cons.Items(Cons.Items(values).Single()).Single();
            _ = await Call(context, Delete, table);
            EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
            // External FUN pointers are canonical VM entries with no ENV.
            EtsFoundation405Access.Require(ReferenceEquals(copied, source)
                && runtime.Functions.IsExternalEntry(copied)
                && copied.ExternalIdentity == source.ExternalIdentity);
            EtsFoundation405Access.Require((await copied.Invoke(context, [Term.List(Term.I(1), Term.I(2))])).Equals(Term.I(2)));

            return true;
        });
    }
}
