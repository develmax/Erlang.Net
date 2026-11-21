using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRepeatedRequiredSequencePrefixChecks
{
    internal const string TestName = "compiler/beam/frees-required-sequence-remainders-while-keeping-decoded-values";
    private const string Fixture = "beam_repeated_required_sequence_prefix_probe.erl";
    private const string IncorrectInstructions = "The emitted function has the wrong register or branch layout.";
    private const string IncorrectResults = "The emitted function returned unexpected values.";

    private const string Source = """
        -module(beam_repeated_required_sequence_prefix_probe).
        -export([decode/2]).

        decode(Input, Tags) ->
            Parts = unpack(Input, Tags),
            [First|Rest] = Parts,
            A = read(First, [2]),
            [Second|Rest2] = Rest,
            B = read(Second, [2]),
            [Third|Rest3] = Rest2,
            C = read(Third, [2]),
            {D,Rest4} = case Rest3 of
                [{4, P}|Tail1] -> {read(P, []), Tail1};
                _ -> {absent, Rest3}
            end,
            {E,Left} = case Rest4 of
                [{5, Q}|Tail2] -> {read(Q, []), Tail2};
                _ -> {absent, Rest4}
            end,
            case Left of
                [] -> true;
                _ -> exit({error,{asn1,{unexpected,Left}}})
            end,
            Result = {fields,A,B,C,D,E},
            Result.

        read(Value, _) -> Value.
        unpack(Input, _) -> Input.
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
            && Items(instruction, "Operands").ToArray() is [_, var atom, _] && atoms[Value(atom)] == "decode"
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        if (body.Where(instruction => Opcode(instruction) == Allocate).Select(instruction =>
                Value(Items(instruction, "Operands").First())).ToArray() is not [6]
            || body.Count(instruction => Opcode(instruction) == InitYRegisters) != 5
            || body.Count(instruction => Opcode(instruction) == BadMatch) != 3)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term a = Term.I(1);
            Term b = Term.I(2);
            Term c = Term.I(3);
            Term d = Term.A("fourth");
            Term e = Term.A("fifth");
            Term absent = Term.A("absent");
            Term complete = Term.List(
                a,
                b,
                c,
                Term.Tuple(Term.I(4), d),
                Term.Tuple(Term.I(5), e)
            );
            Term missing = Term.List(a, b, c);
            if (!Term.Tuple(
                Term.A("fields"),
                a,
                b,
                c,
                d,
                e
            ).Equals(await module.Invoke(
                context,
                "decode",
                complete,
                Nil.Value
            ))
                || !Term.Tuple(
                    Term.A("fields"),
                    a,
                    b,
                    c,
                    absent,
                    absent
                ).Equals(await module.Invoke(
                    context,
                    "decode",
                    missing,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string member) => ((IEnumerable)Property(owner, member)).Cast<object>();
}
