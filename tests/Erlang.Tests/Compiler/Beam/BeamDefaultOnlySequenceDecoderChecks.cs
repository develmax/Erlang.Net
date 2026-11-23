using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDefaultOnlySequenceDecoderChecks
{
    internal const string TestName = "compiler/beam/reuses-default-decoder-field-staging-slots";
    private const string Fixture = "beam_default_only_sequence_decoder_probe.erl";
    private const string IncorrectFrame = "The default decoder did not reuse the current remainder slot for its result.";
    private const string IncorrectFields = "A default, a decoded field or an unexpected remainder changed.";

    private const string Source = """
        -module(beam_default_only_sequence_decoder_probe).
        -export([decode/2]).
        decode(Value, Tags) ->
            Start = parse(Value, Tags),
            {First, More} = case Start of
                [{1, Payload1} | Tail1] -> {read_field(Payload1, first), Tail1};
                _ -> {fallback, Start}
            end,
            {Second, Left} = case More of
                [{2, Payload2} | Tail2] -> {read_field(Payload2, second), Tail2};
                _ -> {{missing, 0}, More}
            end,
            case Left of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Left}}})
            end,
            Result = {record, First, Second},
            Result.
        parse(Value, keep) -> Value.
        read_field(Value, Name) -> {Name, Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "decode" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] allocations = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        object[] splits = body.Where(instruction => Opcode(instruction) == GetList).ToArray();
        if (allocations is not [var allocation] || Items(allocation, "Operands").Select(Value).ToArray() is not [3, 2]
            || splits is not [var firstSplit, var secondSplit]
            || Items(firstSplit, "Operands").Select(Value).ToArray() is not [0, 0, 1]
            || Items(secondSplit, "Operands").Select(Value).ToArray() is not [1, 0, 0])
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term keep = Term.A("keep");
            Term record = Term.A("record");
            Term fallback = Term.A("fallback");
            Term missing = Term.Tuple(Term.A("missing"), Term.I(0));
            Term first = Term.Tuple(Term.I(1), Term.I(7));
            Term second = Term.Tuple(Term.I(2), Term.I(8));
            Term unexpected = Term.List(Term.Tuple(Term.I(9), Term.I(11)));
            if (!Term.Tuple(record, fallback, missing).Equals(await module.Invoke(
                context,
                "decode",
                Nil.Value,
                keep
            ))
                || !Term.Tuple(record, Term.Tuple(Term.A("first"), Term.I(7)), missing).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        Term.List(first),
                        keep
                    ))
                || !Term.Tuple(record, fallback, Term.Tuple(Term.A("second"), Term.I(8))).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        Term.List(second),
                        keep
                    ))
                || !Term.Tuple(record, Term.Tuple(Term.A("first"), Term.I(7)), Term.Tuple(Term.A("second"), Term.I(8))).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        Term.List(first, second),
                        keep
                    )))
            {
                throw new InvalidOperationException(IncorrectFields);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "decode",
                    unexpected,
                    keep
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit
                && error.Reason.Equals(Term.Tuple(Term.A("error"), Term.Tuple(Term.A("asn1"), Term.Tuple(Term.A("unexpected"), unexpected)))))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectFields);
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
