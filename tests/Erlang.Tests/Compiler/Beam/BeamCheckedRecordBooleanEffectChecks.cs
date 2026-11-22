using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedRecordBooleanEffectChecks
{
    internal const string TestName = "compiler/beam/keeps-guarded-record-boolean-effects-in-registers";
    private const string Fixture = "beam_checked_record_boolean_effect_probe.erl";
    private const string WrongLayout = "The nested record effect requires a checked zero-slot frame and a shared constant continuation.";
    private const string WrongOutcome = "The guarded record effect changed a result, failure, or observable call.";

    private const string Source = """
        -module(beam_checked_record_boolean_effect_probe).
        -export([dispatch/4]).
        -record(inner, {flag}).
        -record(outer, {nested}).
        dispatch(internal, {effect, Message, _}, {State, client}, Data)
          when State =:= first; State =:= second; State =:= third ->
            case Data#outer.nested#inner.flag of
                false -> observe(probe_call, [Message]);
                true -> ok
            end,
            unchanged;
        dispatch(internal, {peer, Message, _}, {first, client}, Data) -> {peer, Message, Data};
        dispatch(internal, {peer, Message, _}, {second, client}, Data) -> {peer, Message, Data};
        dispatch(internal, {other}, {first, client}, Data) -> {other, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        observe(_, [raise]) -> erlang:error(probe_call_failed);
        observe(Key, Values) -> erlang:put(Key, Values).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasCheckedEffect(image)
            || HasCheckedEffect(CodeGeneration.CompileBeamModule(Source.Replace(
                "true -> ok",
                "true -> observe(probe_call, [Message])",
                StringComparison.Ordinal
            ), Fixture))
            || HasCheckedEffect(CodeGeneration.CompileBeamModule(Source.Replace(
                "case Data#outer",
                "case\n Data#outer",
                StringComparison.Ordinal
            ), Fixture)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term type = Term.A("internal");
            Term route = Term.Tuple(Term.A("first"), Term.A("client"));
            Term wrongRole = Term.Tuple(Term.A("first"), Term.A("server"));
            Term key = Term.A("probe_call");
            Term payload = Term.A("payload");
            Term unchanged = Term.A("unchanged");
            Term quiet = Data(Term.A(ErlangBooleanAtoms.True));
            Term verbose = Data(Term.A(ErlangBooleanAtoms.False));
            if (!(await Invoke(route, quiet, payload)).Equals(unchanged)
                || context.Dictionary.ContainsKey(key)
                || !(await Invoke(route, verbose, payload)).Equals(unchanged)
                || !context.Dictionary.TryGetValue(key, out Term? logged)
                || !Term.List(payload).Equals(logged))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            Term malformed = Term.A("malformed");
            Term recordError = Term.Tuple(Term.A(RecordAtoms.BadRecord), malformed);
            await Fail(malformed, recordError);
            await Fail(Term.Tuple(Term.A("outer"), malformed), recordError);
            await Fail(Data(malformed), Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), malformed));
            await Fail(verbose, Term.A("probe_call_failed"), Term.A("raise"));
            if (!(await Invoke(wrongRole, malformed, payload)).Equals(Term.Tuple(
                Term.A("fallback"),
                type,
                Event(payload),
                wrongRole,
                malformed
            )))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            return Nil.Value;

            async Task<Term> Invoke(Term state, Term data, Term message) => await module.Invoke(
                context,
                "dispatch",
                type,
                Event(message),
                state,
                data
            );

            async Task Fail(Term data, Term reason, Term? message = null)
            {
                try
                {
                    _ = await Invoke(route, data, message ?? payload);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }
        });
    }

    private static Term Data(Term flag) =>
        Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), flag));

    private static Term Event(Term message) => Term.Tuple(Term.A("effect"), message, Term.A("spare"));

    private static bool HasCheckedEffect(byte[] image)
    {
        object[] instructions = Items(Code(image), "Instructions").ToArray();
        byte[] prefix = [IsTaggedTuple, GetTupleElement, IsTaggedTuple, Allocate, GetTupleElement, SelectValue];
        for (int start = 0; start + prefix.Length <= instructions.Length; start++)
        {
            if (!instructions.Skip(start).Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
                || !Operands(instructions[start + 3]).Select(Value).SequenceEqual([0, 6])
                || Operands(instructions[start + 5]) is not [_, _, _, _, var effect, _, var done])
            {
                continue;
            }

            int run = FindLabel(effect);
            int join = FindLabel(done);
            if (run >= 0 && join > run + 2
                && Opcode(instructions[run + 1]) == TestHeap
                && Operands(instructions[run + 1]).Select(Value).SequenceEqual([2, 6])
                && Opcode(instructions[run + 2]) == PutList
                && Operands(instructions[run + 2]) is [var payload, _, _]
                && Property(payload, "Kind") is "x" && Value(payload) == 5
                && Opcode(instructions[join - 1]) == LocalCall)
            {
                return true;
            }
        }

        return false;

        int FindLabel(object target) => Array.FindIndex(instructions, instruction =>
            Opcode(instruction) == Label && Value(Operands(instruction)[0]) == Value(target));
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
