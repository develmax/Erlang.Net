using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedListQualifierChecks
{
    internal const string TestName = "compiler/beam/lifts-list-helpers-with-ordered-guard-and-expression-filters";
    private const string Source = """
        -module(beam_ordered_list_qualifier).
        -export([run/1, observe/1, protected/1]).

        run(Rows) -> [Row || Row <- Rows, first(Row), element(1, Row) =:= ok, last(Row)].

        first(Row) -> put(events, [{first, Row} | get(events)]), true.
        last({ok, valid}=Row) -> put(events, [{last, Row} | get(events)]), true;
        last({ok, bad}=Row) -> put(events, [{last, Row} | get(events)]), invalid;
        last(Row) -> put(events, [{last, Row} | get(events)]), false.

        observe(Rows) ->
            put(events, []),
            Result = try run(Rows) of Values -> {ok, Values}
                     catch error:Reason -> {error, Reason}
                     end,
            {Result, erase(events)}.

        protected(Rows) ->
            try [Row || Row <- Rows, first(Row), element(1, Row) =:= ok, last(Row)]
            catch error:Reason -> Reason
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_ordered_list_qualifier.erl");
        var generated = ((IEnumerable)Property(Code(image), "Functions")).Cast<object>()
            .Select(item => ((string)Property(item, "Name"), (int)Property(item, "Arity"))).ToHashSet();
        if (!generated.Contains(("-run/1-lc$^0/1-0-", 1))
            || generated.Contains(("-protected/1-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException("Only the unprotected qualifiers belong in the new helper.");
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([ErlangModuleNames.Module]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term valid = Term.Tuple(Term.A("ok"), Term.A("valid"));
            Term rejected = Term.Tuple(Term.A("ok"), Term.A("rejected"));
            Term bad = Term.Tuple(Term.A("ok"), Term.A("bad"));
            Term malformed = Term.A("malformed");
            Term first = Term.A("first");
            Term last = Term.A("last");
            Term failure = Term.Tuple(Term.A("bad_filter"), Term.A("invalid"));
            Term success = Term.Tuple(
                Term.Tuple(Term.A("ok"), Term.List(valid)),
                Term.List(
                    Term.Tuple(last, valid),
                    Term.Tuple(first, valid),
                    Term.Tuple(last, rejected),
                    Term.Tuple(first, rejected),
                    Term.Tuple(first, malformed)
                )
            );
            Term error = Term.Tuple(
                Term.Tuple(Term.A("error"), failure),
                Term.List(Term.Tuple(last, bad), Term.Tuple(first, bad))
            );
            if (!success.Equals(await module.Invoke(context, "observe", [Term.List(malformed, rejected, valid)]))
                || !error.Equals(await module.Invoke(context, "observe", [Term.List(bad, valid)])))
            {
                throw new InvalidOperationException("A list qualifier changed effect order, guard rejection, or bad_filter.");
            }

            return Nil.Value;
        });
    }
}
