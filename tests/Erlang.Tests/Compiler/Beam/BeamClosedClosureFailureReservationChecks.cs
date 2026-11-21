using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamClosedClosureFailureReservationChecks
{
    internal const string TestName = "compiler/beam/reserves-a-closed-closure-s-failure-ordinal";
    private const string MissingHelper = "Closed partial and total functions must leave different ordinals for the captured failure helper.";
    private const string WrongStack = "The captured closure must keep the original failing argument and its reserved failure frame.";
    private const string Source = """
        -module(beam_closure_failure_reservation).
        -export([route/2, total_first/2, invoke/3]).

        route(Values, Extra) ->
            Closed = fun({one, Value}) -> {one, Value} end,
            Result = map_one(Closed, Values),
            Captured = fun({two, Value}) -> {Extra, Value} end,
            {Result, Captured}.

        total_first(Values, Extra) ->
            Closed = fun(Value) -> {one, Value} end,
            Result = map_one(Closed, Values),
            Captured = fun({two, Value}) -> {Extra, Value} end,
            {Result, Captured}.

        invoke(Values, Extra, Input) ->
            {Result, Captured} = route(Values, Extra),
            {Result, Captured(Input)}.

        map_one(Callback, [Value]) ->
            [Callback(Value)].
        """;

    internal static async Task Run()
    {
        const string moduleName = "beam_closure_failure_reservation";
        byte[] image = CodeGeneration.CompileBeamModule(Source, moduleName + ".erl");
        BeamModule module = BeamModule.Prepare(image);
        string[] names = module.Functions.Where(function => function.Name.Contains("-inlined-", StringComparison.Ordinal))
            .Select(function => function.Name).ToArray();
        if (!names.Contains("-route/2-inlined-1-") || names.Contains("-route/2-inlined-0-")
            || !names.Contains("-total_first/2-inlined-0-") || names.Contains("-total_first/2-inlined-1-"))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term inputs = Term.List(Term.Tuple(Term.A("one"), Term.I(5)));
            Term extra = Term.A("captured");
            Term success = await module.Invoke(
                context,
                "invoke",
                inputs,
                extra,
                Term.Tuple(Term.A("two"), Term.I(6))
            );
            if (!Term.Tuple(inputs, Term.Tuple(extra, Term.I(6))).Equals(success))
            {
                throw new InvalidOperationException(WrongStack);
            }

            Term bad = Term.A("malformed");
            try
            {
                await module.Invoke(
                    context,
                    "invoke",
                    inputs,
                    extra,
                    bad
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause"))
                && error.StackTraceTerm is Cons
                {
                    Head: TupleTerm
                    {
                        Items: [Atom moduleAtom, Atom { Name: "-route/2-inlined-1-" }, var arguments, _]
                    }
                } && moduleAtom.Name == moduleName && Term.List(bad).Equals(arguments))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongStack);
        });
    }
}
