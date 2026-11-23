using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamClosedClosureFailureReservationChecks
{
    internal const string TestName = "compiler/beam/reserves-a-closed-closure-s-failure-ordinal";
    private const string ListDependency = "lists";
    private const string MissingHelper = "Closed partial and total functions must leave different ordinals for the captured failure helper.";
    private const string WrongStack = "The captured closure must keep the original failing argument and its reserved failure frame.";
    private const string Source = """
        -module(beam_closure_failure_reservation).
        -import(lists, [map/2]).
        -export([route/2, total_first/2, invoke/3, callback_route/3, callback_total_first/3]).

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

        callback_route(First, Second, Extra) ->
            [map(fun({one, Value}) -> {one, Value} end, First),
             lists:map(fun({two, Value}) -> {Extra, Value} end, Second)].

        callback_total_first(First, Second, Extra) ->
            [map(fun(Value) -> {one, Value} end, First),
             lists:map(fun({two, Value}) -> {Extra, Value} end, Second)].

        map_one(Callback, [Value]) ->
            [Callback(Value)].
        """;

    internal static async Task Run()
    {
        const string moduleName = "beam_closure_failure_reservation";
        byte[] image = CodeGeneration.CompileBeamModule(Source, moduleName + ".erl");
        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([ListDependency]));
        string[] names = module.Functions.Where(function => function.Name.Contains("-inlined-", StringComparison.Ordinal))
            .Select(function => function.Name).ToArray();
        if (!names.Contains("-route/2-inlined-1-") || names.Contains("-route/2-inlined-0-")
            || !names.Contains("-total_first/2-inlined-0-") || names.Contains("-total_first/2-inlined-1-")
            || !names.Contains("-callback_route/3-inlined-1-") || names.Contains("-callback_route/3-inlined-0-")
            || !names.Contains("-callback_total_first/3-inlined-0-") || names.Contains("-callback_total_first/3-inlined-1-"))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        await using var runtime = await ProcessRuntime.CreateAsync(
            new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed(ListDependency)])
        );
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

            Term second = Term.Tuple(Term.A("two"), Term.I(6));
            Term callbackResult = await module.Invoke(
                context,
                "callback_route",
                inputs,
                Term.List(second),
                extra
            );
            if (!Term.List(inputs, Term.List(Term.Tuple(extra, Term.I(6)))).Equals(callbackResult))
            {
                throw new InvalidOperationException(WrongStack);
            }

            Term bad = Term.A("malformed");
            await CheckFailure(
                "callback_route",
                "-callback_route/3-inlined-1-",
                [inputs, Term.List(bad), extra]
            );
            await CheckFailure("invoke", "-route/2-inlined-1-", [inputs, extra, bad]);

            return Nil.Value;

            async Task CheckFailure(string function, string helper, Term[] arguments)
            {
                try
                {
                    await module.Invoke(context, function, arguments);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause"))
                    && error.StackTraceTerm is Cons
                    {
                        Head: TupleTerm
                        {
                            Items: [Atom moduleAtom, Atom helperAtom, var originalArguments, _]
                        }
                    } && moduleAtom.Name == moduleName && helperAtom.Name == helper && Term.List(bad).Equals(originalArguments))
                {
                    return;
                }

                throw new InvalidOperationException(WrongStack);
            }
        });
    }
}
