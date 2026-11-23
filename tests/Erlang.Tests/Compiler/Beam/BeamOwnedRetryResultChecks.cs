using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedRetryResultChecks
{
    internal const string TestName = "compiler/beam/reuses-checked-retry-frame-for-the-whole-call-result";
    private const string Fixture = "beam_owned_retry_result_probe.erl";
    private const string WrongLayout = "The result case must own two slots, keep the whole selector for failure, and trim before either terminal call.";
    private const string WrongOutcome = "The owned retry case changed a checked terminal result or original error value.";

    private const string Source = """
        -module(beam_owned_retry_result_probe).
        -export([dispatch/4]).
        -record(inner, {items}).
        -record(outer, {nested, marker}).
        dispatch(internal, {failure, _, _}, {retry, client}, #outer{nested=#inner{items=[]}}) ->
            first;
        dispatch(internal, {failure, Payload, _}, {retry, client}, Outer=#outer{nested=Inner}) ->
            Updated = case Inner#inner.items of
                refresh -> Inner#inner{items=change(Payload, marker)};
                _ -> Inner
            end,
            case select_reply(Updated) of
                {terminate, Code, Replacement} ->
                    {A, B} = commit(Code, format(marker, [Outer#outer.marker]),
                                    domain, 42, {retry, client}, Outer#outer{nested=Replacement}),
                    {stop, A, B};
                {"primary", {Message, Replacement}} ->
                    Response = emit(Message, Outer#outer{nested=Replacement}),
                    {next, Response};
                {_Method, {Message, Replacement}} ->
                    Response = emit(Message, Outer#outer{nested=Replacement}),
                    {keep, Response}
            end;
        dispatch(internal, {failure, _, _}, {state_b, client}, Data) -> {owned_b, Data};
        dispatch(internal, {failure, _, _}, {state_c, client}, Data) -> {owned_c, Data};
        dispatch(internal, {peer, Payload, _}, {state_b, client}, Data) -> {peer_b, Payload, Data};
        dispatch(internal, {peer, Payload, _}, {state_c, client}, Data) -> {peer_c, Payload, Data};
        dispatch(internal, {small}, {state_b, client}, Data) -> {small, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        change(Payload, marker) -> Payload.
        select_reply(#inner{items=Reply}) -> Reply.
        format(marker, [Value]) -> Value.
        commit(wanted, Text, domain, 42, {retry,client}, Carrier) -> {Text, Carrier};
        commit(malformed, _, domain, 42, {retry,client}, _) -> malformed_reply.
        emit(Message, Carrier) -> {sent, Message, Carrier}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasOwnedResult(image) || HasOwnedResult(CodeGeneration.CompileBeamModule(Source.Replace(
            "{keep, Response}",
            "{keep, Message, Response}",
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
            Term data = Term.A("replacement");
            Term message = Term.A("message");
            Term marker = Term.A("marker");
            Term outer = Term.Tuple(Term.A("outer"), data, marker);
            Term nested = Term.Tuple(message, data);
            Term exact = Term.String("primary");
            Term pair = Term.Tuple(exact, nested);
            Term sent = Term.Tuple(Term.A("sent"), message, outer);
            if (!(await Invoke(pair)).Equals(Term.Tuple(Term.A("next"), sent))
                || !(await Invoke(Term.Tuple(Term.A("other"), nested))).Equals(Term.Tuple(Term.A("keep"), sent))
                || !(await Invoke(Term.Tuple(Term.A("terminate"), Term.A("wanted"), data)))
                    .Equals(Term.Tuple(Term.A("stop"), marker, outer)))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            await Fail(
                Term.Tuple(Term.A("terminate"), Term.A("malformed"), data),
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Term.A("malformed_reply"))
            );
            foreach (Term malformed in (Term[])
            [
                Term.Tuple(exact, Term.A("not_a_tuple")),
                Term.Tuple(exact, Term.Tuple(message)),
                Term.Tuple(Term.A("wrong_tag"), Term.A("wanted"), data),
                Term.A("malformed")
            ])
            {
                await Fail(malformed, Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), malformed));
            }

            return Nil.Value;

            async Task<Term> Invoke(Term reply) => await module.Invoke(
                context,
                "dispatch",
                Term.A("internal"),
                Term.Tuple(Term.A("failure"), Term.A("payload"), Term.A("spare")),
                Term.Tuple(Term.A("retry"), Term.A("client")),
                Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), reply), marker)
            );

            async Task Fail(Term reply, Term reason)
            {
                try
                {
                    _ = await Invoke(reply);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }
        });
    }

    private static bool HasOwnedResult(byte[] image)
    {
        object[] instructions = Items(Code(image), "Instructions").ToArray();
        int dispatch = Array.FindIndex(
            instructions,
            instruction =>
            Opcode(instruction) == SelectTupleArity
            && Operands(instruction) is [_, _, _, var pair, _, var triple, _]
            && Value(pair) == 2 && Value(triple) == 3
        );
        if (dispatch < 2 || Opcode(instructions[dispatch - 1]) != IsTuple
            || Opcode(instructions[dispatch - 2]) != Move
            || Operands(instructions[dispatch - 2]) is not [var source, var saved]
            || !IsRegister(source, "x", 0) || !IsRegister(saved, "y", 0))
        {
            return false;
        }

        object[] body = instructions.Skip(dispatch + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();

        return instructions.Take(dispatch).Any(instruction =>
            Opcode(instruction) == Allocate && Operands(instruction).Select(Value).SequenceEqual([2, 6]))
            && body.Count(instruction =>
                Opcode(instruction) == TrimFrame && Operands(instruction).Select(Value).SequenceEqual([2, 0])) == 3
            && body.Any(instruction =>
                Opcode(instruction) == CaseEnd && Operands(instruction) is [var failed] && IsRegister(failed, "y", 0));
    }

    private static bool IsRegister(object operand, string kind, int index) =>
        Equals(Property(operand, "Kind"), kind) && Value(operand) == index;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
