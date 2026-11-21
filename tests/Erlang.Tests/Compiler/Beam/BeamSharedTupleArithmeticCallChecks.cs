using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedTupleArithmeticCallChecks
{
    internal const string TestName = "compiler/beam/delays-tuple-fallback-frame-until-arithmetic-succeeds";
    private const string Fixture = "beam_tuple_arithmetic_call_probe.erl";
    private const string WrongFrame = "The checked tuple fallback must allocate after its first arithmetic operation.";
    private const string WrongSlots = "The fallback frame must preserve the shift count and the arithmetic result.";
    private const string WrongControl = "A guarded source clause used the unguarded tuple continuation.";
    private const string Source = """
        -module(beam_tuple_arithmetic_call_probe).
        -export([run/1, control/1, seed/0]).

        run({_,17}) ->
            seed();
        run({Base,Width}) ->
            (Base bsl Width) bor (seed() band (1 bsl Width - 1)).

        control({Tag,17}) when Tag =:= allowed ->
            seed();
        control({Base,Width}) ->
            (Base bsl Width) bor (seed() band (1 bsl Width - 1)).

        seed() -> erlang:put(observed, next).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = Body("run");
        int arithmetic = Array.FindIndex(body, instruction => Opcode(instruction) == GcBifTwo);
        int allocate = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int tail = Array.FindIndex(body, instruction => Opcode(instruction) == TailLocalCall);
        if (tail < 0 || arithmetic <= tail || allocate != arithmetic + 1)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] frame = Operands(body[allocate]);
        if (Value(frame[0]) != 2 || Value(frame[1]) != 2
            || !Moves(body[allocate + 1], 1, 0) || !Moves(body[allocate + 2], 0, 1))
        {
            throw new InvalidOperationException(WrongSlots);
        }

        object[] control = Body("control");
        if (Array.FindIndex(control, instruction => Opcode(instruction) == Allocate)
            >= Array.FindIndex(control, instruction => Opcode(instruction) == GcBifTwo))
        {
            throw new InvalidOperationException(WrongControl);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool Moves(object instruction, int from, int to)
    {
        if (Opcode(instruction) != Move)
        {
            return false;
        }

        object[] operands = Operands(instruction);

        return Value(operands[0]) == from && Value(operands[1]) == to;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Children(instruction, "Operands").ToArray();

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
