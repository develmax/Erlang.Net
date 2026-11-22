using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedRetryOptionalUpdateChecks
{
    internal const string TestName = "compiler/beam/keeps-checked-retry-update-operands-in-registers";
    private const string Fixture = "beam_checked_retry_optional_update_probe.erl";
    private const int NonNilTypeBits = 0x1f7f;
    private const string WrongLayout = "The checked selector and optional update argument must reach the call without a stack spill.";
    private const string WrongOutcome = "The checked optional update changed a retry result, skipped call, or source error.";

    private const string Source = """
        -module(beam_checked_retry_optional_update_probe).
        -export([dispatch/4]).
        -record(inner, {items}).
        -record(outer, {nested}).
        dispatch(internal, {failure, _, _}, {retry, client}, #outer{nested=#inner{items=[]}}) ->
            first;
        dispatch(internal, {failure, Payload, _}, {retry, client}, Outer=#outer{nested=Inner}) ->
            Updated = case Inner#inner.items of
                refresh -> Inner#inner{items=change(Payload, marker)};
                _ -> Inner
            end,
            case inspect(Updated) of
                {value, Value} -> {done, Value, Outer}
            end;
        dispatch(internal, {failure, _, _}, {state_b, client}, Data) -> {owned_b, Data};
        dispatch(internal, {failure, _, _}, {state_c, client}, Data) -> {owned_c, Data};
        dispatch(internal, {peer, Payload, _}, {state_b, client}, Data) -> {peer_b, Payload, Data};
        dispatch(internal, {peer, Payload, _}, {state_c, client}, Data) -> {peer_c, Payload, Data};
        dispatch(internal, {small}, {state_b, client}, Data) -> {small, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        change(raise, marker) -> erlang:error(producer_error);
        change(Payload, marker) -> Payload.
        inspect(#inner{items=malformed}) -> malformed;
        inspect(#inner{items=Value}) -> {value, Value}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasRegisterPrefix(image, requireNonNilType: true) || HasRegisterPrefix(CodeGeneration.CompileBeamModule(Source.Replace(
            "{done, Value, Outer}",
            "{done, Value, Outer, Inner}",
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
            Term refresh = Data(Term.A("refresh"));
            Term original = Term.A("original");
            Term skipped = Data(original);
            Term payload = Term.A("payload");
            Term raise = Term.A("raise");
            if (!(await Invoke(refresh, payload)).Equals(Term.Tuple(Term.A("done"), payload, refresh))
                || !(await Invoke(skipped, raise)).Equals(Term.Tuple(Term.A("done"), original, skipped))
                || !(await Invoke(Data(Nil.Value), raise)).Equals(Term.A("first")))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            Term malformed = Term.A("malformed");
            await Fail(refresh, raise, Term.A("producer_error"));
            await Fail(refresh, malformed, Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), malformed));

            return Nil.Value;

            async Task<Term> Invoke(Term data, Term message) => await module.Invoke(
                context,
                "dispatch",
                Term.A("internal"),
                Term.Tuple(Term.A("failure"), message, Term.A("spare")),
                Term.Tuple(Term.A("retry"), Term.A("client")),
                data
            );

            async Task Fail(Term data, Term message, Term reason)
            {
                try
                {
                    _ = await Invoke(data, message);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }
        });
    }

    private static Term Data(Term value) => Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), value));

    private static bool HasRegisterPrefix(byte[] image, bool requireNonNilType = false)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        for (int index = 0; index + 4 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) != ExactEqualTest
                || Operands(instructions[index]) is not [_, var selector, var key]
                || !IsRegister(selector, "x", 1) || Property(key, "Kind") is not "atom"
                || atoms[Value(key)] != "refresh")
            {
                continue;
            }

            object[] arguments = instructions.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) != LocalCall).ToArray();
            if (arguments.Length <= 3 && arguments.Any(instruction =>
                Opcode(instruction) == Move && Operands(instruction) is [var read, var output]
                && IsRegister(read, "x", 5) && IsRegister(output, "x", 0)))
            {
                return !requireNonNilType || Property(selector, "TypeIndex") is int type
                    && BeamAdjacentNumericResultChecks.ReadOperandTypeBits(image, type) == NonNilTypeBits;
            }
        }

        return false;
    }

    private static bool IsRegister(object operand, string kind, int index) =>
        Equals(Property(operand, "Kind"), kind) && Value(operand) == index;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
