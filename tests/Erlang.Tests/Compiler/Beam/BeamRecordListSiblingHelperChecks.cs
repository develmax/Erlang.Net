using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordListSiblingHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-list-siblings-in-tuple-record-field-order";
    private const string Fixture = "beam_record_list_sibling_helpers.erl";
    private const string WrongOrder = "Tuple-record traversal changed the expected comprehension helper order.";
    private const string WrongValue = "Tuple-record traversal changed field values or effect order.";

    private const string Source = """
        -module(beam_record_list_sibling_helpers).
        -export([permuted/2, bound_then_record/2, wildcard/2, reset/0, events/0]).
        -record(row, {first, second, tag = fixed}).

        permuted(First, Second) ->
            #row{second = [remember(second, X) || X <- Second],
                 first = [remember(first, X) || X <- First]}.

        bound_then_record(First, Second) ->
            Seen = [remember(first, X) || X <- First],
            #row{second = [remember(second, X) || X <- Second], first = Seen}.

        wildcard(First, Second) ->
            #row{second = [remember(second, X) || X <- Second],
                 first = [remember(first, X) || X <- First],
                 _ = remember(wildcard, fallback)}.

        remember(Side, Value) ->
            put(events, [Side | get(events)]),
            Value.
        reset() -> put(events, []), ok.
        events() -> get(events).
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .ToArray();
        if (!Helpers("permuted").SequenceEqual([("-permuted/2-lc$^0/1-1-", 1), ("-permuted/2-lc$^1/1-0-", 1)])
            || !Helpers("bound_then_record").SequenceEqual([("-bound_then_record/2-lc$^1/1-1-", 1), ("-bound_then_record/2-lc$^0/1-0-", 1)])
            || Helpers("wildcard").Any())
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term first = Term.List(alpha);
            Term second = Term.List(beta);
            Term fixedTag = Term.A("fixed");
            Term wildcard = Term.A("wildcard");
            Term fallback = Term.A("fallback");
            Term firstEffect = Term.A("first");
            Term secondEffect = Term.A("second");
            foreach (string function in new[] { "permuted", "bound_then_record", "wildcard" })
            {
                bool usesWildcard = function == "wildcard";
                Term tuple = Term.Tuple(
                    Term.A("row"),
                    first,
                    second,
                    usesWildcard ? fallback : fixedTag
                );
                Term effects = usesWildcard ? Term.List(wildcard, secondEffect, firstEffect)
                    : Term.List(secondEffect, firstEffect);

                await module.Invoke(context, "reset");
                if (!tuple.Equals(await module.Invoke(
                    context,
                    function,
                    first,
                    second
                ))
                    || !effects.Equals(await module.Invoke(context, "events")))
                {
                    throw new InvalidOperationException(WrongValue + " " + function);
                }
            }

            return Nil.Value;
        });

        IEnumerable<(string Name, int Arity)> Helpers(string function) => functions
            .Where(helper => helper.Name.StartsWith("-" + function + "/2-lc$^", StringComparison.Ordinal));
    }
}
