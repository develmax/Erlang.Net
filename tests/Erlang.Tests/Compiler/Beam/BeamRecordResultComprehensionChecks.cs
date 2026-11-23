using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordResultComprehensionChecks
{
    internal const string TestName = "compiler/beam/lifts-flat-tuple-record-list-results";
    private const string Fixture = "beam_record_result_comprehension.erl";
    private const string WrongTopology = "Tuple-record list results changed the expected helper topology.";
    private const string WrongValue = "Tuple-record list results changed field values or effect order.";

    private const string Source = """
        -module(beam_record_result_comprehension).
        -export([plain/2, wrapped/1, active_default/1, wildcard/1, reset/0, events/0]).
        -record(row, {first = tag, second = default}).
        -record(active, {first = mark(default, seed), second}).
        plain(Rows, Tag) ->
            [#row{second = mark(second, X), first = mark(first, Tag)} || X <- Rows].
        wrapped(Rows) ->
            [wrap(#row{second = mark(value, X)}) || X <- Rows].
        active_default(Rows) -> [#active{second = X} || X <- Rows].
        wildcard(Rows) -> [#row{first = X, _ = mark(wild, X)} || X <- Rows].
        mark(Key, Value) ->
            erlang:put(events, [{Key, Value} | erlang:get(events)]),
            Value.
        wrap(Row) -> {wrapped, Row}.
        reset() -> erlang:put(events, []), ok.
        events() -> erlang:get(events).
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .ToHashSet();
        if (!functions.Contains(("-plain/2-lc$^0/1-0-", 2))
            || !functions.Contains(("-wrapped/1-lc$^0/1-0-", 1))
            || functions.Any(function => function.Name.StartsWith("-active_default/1-lc$", StringComparison.Ordinal)
                || function.Name.StartsWith("-wildcard/1-lc$", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(WrongTopology);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("alpha");
            Term b = Term.A("beta");
            Term tag = Term.A("tag");
            Term row = Term.A("row");
            Term wrapped = Term.A("wrapped");
            Term active = Term.A("active");
            Term seed = Term.A("seed");
            Term rows = Term.List(a, b);
            Term recordA = Term.Tuple(row, tag, a);
            Term recordB = Term.Tuple(row, tag, b);
            Term first = Term.Tuple(Term.A("first"), tag);

            await module.Invoke(context, "reset");
            Require(Term.List(recordA, recordB), await module.Invoke(
                context,
                "plain",
                rows,
                tag
            ));
            Require(
                Term.List(
                    Term.Tuple(Term.A("second"), b),
                    first,
                    Term.Tuple(Term.A("second"), a),
                    first
                ),
                await module.Invoke(context, "events")
            );

            await module.Invoke(context, "reset");
            Require(Term.List(Term.Tuple(wrapped, recordA), Term.Tuple(wrapped, recordB)), await module.Invoke(
                context,
                "wrapped",
                rows
            ));
            Require(Term.List(Term.Tuple(Term.A("value"), b), Term.Tuple(Term.A("value"), a)), await module.Invoke(context, "events"));

            await module.Invoke(context, "reset");
            Require(Term.List(Term.Tuple(active, seed, a), Term.Tuple(active, seed, b)), await module.Invoke(
                context,
                "active_default",
                rows
            ));
            Term defaultEffect = Term.Tuple(Term.A("default"), seed);
            Require(Term.List(defaultEffect, defaultEffect), await module.Invoke(context, "events"));

            await module.Invoke(context, "reset");
            Require(Term.List(Term.Tuple(row, a, a), Term.Tuple(row, b, b)), await module.Invoke(
                context,
                "wildcard",
                rows
            ));
            Require(Term.List(Term.Tuple(Term.A("wild"), b), Term.Tuple(Term.A("wild"), a)), await module.Invoke(context, "events"));

            return Nil.Value;
        });
    }

    private static void Require(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongValue);
        }
    }
}
