using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedNilStatePairResultChecks
{
    internal const string TestName = "compiler/beam/retains-only-checked-record-across-nil-reply-calls";
    private const string Fixture = "beam_checked_nil_state_pair_result_probe.erl";
    private const string WrongLayout = "The checked nil result must use one slot through the first call and trim it before checking the second result.";
    private const string WrongOutcome = "The checked nil result changed a callback argument, source error, or retry result.";

    private const string Source = """
        -module(beam_checked_nil_state_pair_result_probe).
        -export([dispatch/4]).
        -record(inner, {items}).
        -record(outer, {nested, marker}).
        dispatch(internal, {failure, _, _}, Route={retry, client},
                 Outer=#outer{nested=#inner{items=[]}}) ->
            {Begin, End} = consume(prefix, build(format, [Outer#outer.marker]), marker, 42, Route, Outer),
            {stop, Begin, End};
        dispatch(internal, {failure, Payload, _}, {retry, client}=Route,
                 Outer=#outer{nested=Inner}) ->
            case Inner#inner.items of
                alternate -> {alternate, Route, Payload, Outer};
                _ -> {second, Route, Payload, Inner}
            end;
        dispatch(internal, {failure, _, _}, {state_b, client}, Data) -> {owned_b, Data};
        dispatch(internal, {failure, _, _}, {state_c, client}, Data) -> {owned_c, Data};
        dispatch(internal, {peer, Payload, _}, {state_b, client}, Data) -> {peer_b, Payload, Data};
        dispatch(internal, {peer, Payload, _}, {state_c, client}, Data) -> {peer_c, Payload, Data};
        dispatch(internal, {small}, {state_b, client}, Data) -> {small, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        build(format, [producer_error]) -> erlang:error(producer_error);
        build(Tag, [Value]) -> {Tag, Value}.
        consume(prefix, {format, malformed}, marker, 42, _, _) -> malformed;
        consume(prefix, Produced, marker, 42, Route, Data) ->
            {{Produced, Route}, Data}.
        inspect(Route) -> Route.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasSingleSlotResult(image)
            || HasSingleSlotResult(CodeGeneration.CompileBeamModule(Source.Replace(
                "42, Route, Outer),",
                "42, inspect(Route), Outer),",
                StringComparison.Ordinal
            ), Fixture))
            || HasSingleSlotResult(CodeGeneration.CompileBeamModule(Source.Replace(
                "{stop, Begin, End};",
                "{stop, Begin, Begin};",
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
            Term route = Term.Tuple(Term.A("retry"), Term.A("client"));
            Term payload = Term.A("payload");
            Term message = Term.Tuple(Term.A("failure"), payload, Term.A("spare"));
            Term outer = Data(Nil.Value, payload);
            Term expected = Term.Tuple(
                Term.A("stop"),
                Term.Tuple(Term.Tuple(Term.A("format"), payload), route),
                outer
            );
            Term alternate = Data(Term.A("alternate"), payload);
            if (!(await Invoke(outer)).Equals(expected)
                || !(await Invoke(alternate)).Equals(Term.Tuple(
                    Term.A("alternate"),
                    route,
                    payload,
                    alternate
                )))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            Term malformed = Term.A("malformed");
            await Fail(Data(Nil.Value, malformed), Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), malformed));
            Term producerError = Term.A("producer_error");
            await Fail(Data(Nil.Value, producerError), producerError);

            return Nil.Value;

            async Task<Term> Invoke(Term data) => await module.Invoke(
                context,
                "dispatch",
                Term.A("internal"),
                message,
                route,
                data
            );

            async Task Fail(Term data, Term reason)
            {
                try
                {
                    _ = await Invoke(data);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }
        });
    }

    private static Term Data(Term value, Term marker) =>
        Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), value), marker);

    private static bool HasSingleSlotResult(byte[] image)
    {
        object[] instructions = Items(Code(image), "Instructions").ToArray();
        byte[] prefix = [IsNil, AllocateHeap, Move, GetTupleElement, PutList];
        for (int start = 0; start + prefix.Length <= instructions.Length; start++)
        {
            if (!instructions.Skip(start).Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
                || !Operands(instructions[start + 1]).Select(Value).SequenceEqual([1, 2, 4]))
            {
                continue;
            }

            byte[] leaf = instructions.Skip(start + prefix.Length).TakeWhile(instruction => Opcode(instruction) != Return)
                .Select(Opcode).ToArray();
            if (leaf.Count(opcode => opcode == LocalCall) == 2
                && Array.IndexOf(leaf, LocalCall) < Array.IndexOf(leaf, TrimFrame)
                && Array.IndexOf(leaf, TrimFrame) < Array.LastIndexOf(leaf, LocalCall)
                && leaf.TakeLast(7).SequenceEqual([IsTuple, TestArity, TestHeap, GetTupleElement, GetTupleElement, PutTuple, Deallocate]))
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
