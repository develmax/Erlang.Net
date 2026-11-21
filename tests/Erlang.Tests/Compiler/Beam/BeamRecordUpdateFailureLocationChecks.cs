using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamRecordUpdateFailureLocationChecks
{
    internal const string TestName = "compiler/beam/uses-tuple-record-update-field-failure-location";
    private const string Fixture = "beam_record_update_failure_location.erl";
    private const string WrongLocation = "A checked tuple-record update did not use its pinned field annotation.";
    private const string WrongValue = "A valid checked tuple-record update returned a different value.";
    private const string Source = """
        -module(beam_record_update_failure_location).
        -export([ordered/1, nested/1, checked/1]).
        -record(row,{first,second}).

        ordered(Value) ->
            Value#row{second=second,
                      first=first}.

        nested(Value) ->
            erlang:put(visited,
                Value#row{
                    second=second
                }).

        checked(Value) when is_record(Value,row) ->
            Value#row{
                second=second
            }.
        """;

    internal static async Task Run()
    {
        BeamModule module = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Source, Fixture));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term wrong = Term.A("wrong");
            await CheckFailure("ordered", Term.Tuple(Term.A("badrecord"), wrong), 7);
            await CheckFailure("nested", Term.Tuple(Term.A("badrecord"), wrong), 12);
            await CheckFailure("checked", Term.A("function_clause"), 15);

            Term valid = Term.Tuple(Term.A("row"), Term.A("initial_first"), Term.A("initial_second"));
            Term ordered = Term.Tuple(Term.A("row"), Term.A("first"), Term.A("second"));
            Term checkedResult = Term.Tuple(Term.A("row"), Term.A("initial_first"), Term.A("second"));
            if (!ordered.Equals(await module.Invoke(context, "ordered", valid))
                || !checkedResult.Equals(await module.Invoke(context, "checked", valid)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task CheckFailure(string name, Term reason, int line)
            {
                try
                {
                    _ = await module.Invoke(context, name, wrong);
                }
                catch (ErlangException error)
                {
                    if (!reason.Equals(error.Reason)
                        || error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom function, _, var position] } }
                        || function.Name != name
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("file"), Term.String(Fixture)))
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
                    {
                        throw new InvalidOperationException(WrongLocation, error);
                    }

                    return;
                }

                throw new InvalidOperationException(WrongLocation);
            }
        });
    }
}
