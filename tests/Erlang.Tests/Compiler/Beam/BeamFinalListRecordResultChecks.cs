using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFinalListRecordResultChecks
{
    internal const string TestName = "compiler/beam/types-compacted-list-helper-record-operand";
    private const string Fixture = "beam_final_list_record_result_probe.erl";
    private const int ProperListType = 0x0084;
    private const string WrongLayout = "The proven list helper must type its register operand at the final nested record update.";
    private const string WrongOutcome = "The list helper type changed a successful result or original generator error.";

    private const string Source = """
        -module(beam_final_list_record_result_probe).
        -export([dispatch/4]).
        -record(inner, {rows}).
        -record(outer, {nested}).
        dispatch(event, {failure, _}, {waiting, client}, Outer=#outer{nested=Inner}) ->
            Rows = [{Key,Value} || {Key,Value} <- Inner#inner.rows, Key =/= skip],
            Result = Outer#outer{nested=Inner#inner{rows=Rows}},
            {next_state, {ready,client}, Result, [postpone]};
        dispatch(event, {failure, _}, {ready, client}, Data) -> {ready, Data};
        dispatch(event, {single}, _, Data) -> {single, Data};
        dispatch(Event, Message, State, Data) -> {fallback, Event, Message, State, Data}.
        """;

    internal static async Task Run()
    {
        byte[] proven = CodeGeneration.CompileBeamModule(Source, Fixture);
        string unprovedSource = Source.Replace(
            "[{Key,Value} || {Key,Value} <- Inner#inner.rows, Key =/= skip]",
            "'-dispatch/4-lc$^0/1-0-'(Inner#inner.rows)",
            StringComparison.Ordinal
        ) + "\n'-dispatch/4-lc$^0/1-0-'(Value) -> Value.\n";
        byte[] unproved = CodeGeneration.CompileBeamModule(unprovedSource, Fixture);
        if (!HasListInput(proven) || HasListInput(unproved))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var filtered = BeamModule.Prepare(proven);
        var uncheckedResult = BeamModule.Prepare(unproved);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term keep = Term.Tuple(Term.A("keep"), Term.I(1));
            Term skip = Term.Tuple(Term.A("skip"), Term.I(2));
            Term malformed = Term.A("bad_rows");
            Term invalid = new Cons(keep, malformed);
            if (!(await Invoke(filtered, Term.List(keep, skip))).Equals(Next(Term.List(keep)))
                || !(await Invoke(uncheckedResult, invalid)).Equals(Next(invalid)))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            try
            {
                _ = await Invoke(filtered, malformed);
            }
            catch (ErlangException error) when (
                error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), malformed))
            )
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongOutcome);

            async Task<Term> Invoke(BeamModule module, Term rows) => await module.Invoke(
                context,
                "dispatch",
                Term.A("event"),
                Term.Tuple(Term.A("failure"), Term.A("spare")),
                Term.Tuple(Term.A("waiting"), Term.A("client")),
                Record(rows)
            );
        });
    }

    private static Term Next(Term rows) => Term.Tuple(
        Term.A("next_state"),
        Term.Tuple(Term.A("ready"), Term.A("client")),
        Record(rows),
        Term.List(Term.A("postpone"))
    );

    private static Term Record(Term rows) => Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), rows));

    private static bool HasListInput(byte[] image)
    {
        var body = Items(Code(image), "Instructions").ToArray();
        for (int index = 2; index < body.Length; index++)
        {
            if (Opcode(body[index - 2]) == LocalCall && Opcode(body[index - 1]) == TestHeap
                && Opcode(body[index]) == UpdateRecord
                && Items(body[index], "Operands").ToArray() is [_, _, _, _, _, _, var replacement]
                && Property(replacement, "Kind") is "x"
                && (int)(BigInteger)Property(replacement, "Value") == 0
                && Property(replacement, "TypeIndex") is int type
                && BeamAdjacentNumericResultChecks.ReadOperandTypeBits(image, type) == ProperListType)
            {
                return true;
            }
        }

        return false;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
