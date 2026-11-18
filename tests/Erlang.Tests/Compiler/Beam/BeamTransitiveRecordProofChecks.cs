using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTransitiveRecordProofChecks
{
    private const string File = "beam_transitive_record_checks.erl";
    private const string Source = """
        -module(beam_transitive_record_checks).
        -export([checked/1]).
        -record(entry,{value}).

        checked(Record = #entry{}) -> relay(tag, Record).
        relay(Tag, Record) -> inspect(Record, Tag).
        inspect(#entry{value=Value}, Tag) -> {Tag, Value}.
        """;
    private const string ExtraExport = "-export([relay/2]).";
    private const string ClosureExport = "-export([escape/0]).";
    private const string EscapedRelay = "escape() -> fun relay/2.";
    private const string Cycle = """
        -module(beam_transitive_record_checks).
        -export([checked/1]).
        -record(entry,{value}).

        checked(Record = #entry{}) -> inspect(Record, stop).
        inspect(Record = #entry{value=Value}, Mode) ->
            case Mode of
                stop -> Value;
                again -> relay(Record)
            end.
        relay(Record) -> inspect(Record, stop).
        """;

    private const string Interior = """
        -module(beam_transitive_record_checks).
        -export([checked/2]).
        -record(entry,{value}).

        checked(Record = #entry{}, Mode) -> relay(Mode, Record).
        relay(Mode, Record) -> inspect(Record, Mode).
        inspect(Record, keep) -> Record;
        inspect(#entry{value=Value}, read) -> Value.
        """;
    private const string Overwritten = """
        -module(beam_transitive_record_checks).
        -export([checked/2]).
        -record(entry,{value}).

        checked(Record = #entry{}, Mode) -> relay(Mode, Record).
        relay(Mode, Record) -> inspect(Record, Mode).
        inspect(Record, keep) -> Record;
        inspect(Record, read) ->
            Previous = put(record, Record),
            case Previous of
                #entry{value=Value} -> Value;
                _ -> missing
            end.
        """;
    private const string WithoutCallers = """
        -module(beam_transitive_record_checks).
        -export([identity/1]).
        -record(entry,{value}).

        identity(Value) -> Value.
        inspect(Record, keep) -> Record;
        inspect(#entry{value=Value}, read) -> Value.
        """;

    internal static async Task Run()
    {
        BeamModule image = Check(Source, 1);
        string exported = Source.Replace("-export([checked/1]).", "-export([checked/1]). " + ExtraExport, StringComparison.Ordinal);
        BeamModule visible = Check(exported, 2);
        string escaped = Source.Replace("-export([checked/1]).", "-export([checked/1]). " + ClosureExport, StringComparison.Ordinal);
        Check(escaped + "\n" + EscapedRelay, 2);
        Check(Cycle, 2);
        BeamModule interior = Check(Interior, 1);
        BeamModule overwritten = Check(Overwritten, 2);
        Check(WithoutCallers, 1);

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("value");
            Term record = Term.Tuple(Term.A("entry"), value);
            Term expected = Term.Tuple(Term.A("tag"), value);
            if (!expected.Equals(await image.Invoke(context, "checked", record))
                || !expected.Equals(await visible.Invoke(
                    context,
                    "relay",
                    Term.A("tag"),
                    record
                ))
                || !record.Equals(await interior.Invoke(
                    context,
                    "checked",
                    record,
                    Term.A("keep")
                ))
                || !value.Equals(await interior.Invoke(
                    context,
                    "checked",
                    record,
                    Term.A("read")
                ))
                || !Term.A("missing").Equals(await overwritten.Invoke(
                    context,
                    "checked",
                    record,
                    Term.A("read")
                )))
            {
                throw new InvalidOperationException(BeamTransitiveRecordProofDiagnostics.ForwardedRecordChanged);
            }

            try
            {
                await visible.Invoke(
                    context,
                    "relay",
                    Term.A("tag"),
                    Term.Tuple(Term.A("wrong"), value)
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BeamTransitiveRecordProofDiagnostics.UncheckedRecordAccepted);
        });
    }

    private static BeamModule Check(string source, int expectedChecks)
    {
        byte[] beam = CodeGeneration.CompileBeamModule(source, File);
        object code = Code(beam);
        int checks = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Count(instruction => (string)Property(Property(instruction, "Definition"), "Name") == "is_tagged_tuple");
        if (checks != expectedChecks)
        {
            throw new InvalidOperationException(BeamTransitiveRecordProofDiagnostics.RecordCheckCount(expectedChecks, checks));
        }

        return BeamModule.Prepare(beam);
    }
}
