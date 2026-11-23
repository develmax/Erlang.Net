using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCallArithmeticFanoutChecks
{
    internal const string TestName = "compiler/beam/retains-a-call-result-across-independent-integer-operations";
    private const string Fixture = "beam_arithmetic_fanout_probe.erl";
    private const string LostRoots = "The call result or preceding arithmetic output was absent from the live X prefix.";
    private const string IncorrectExchange = "Saved unary call input did not exchange with the preceding result.";

    private const string Source = """
        -module(beam_arithmetic_fanout_probe).
        -export([render/1,control/1]).

        render(Host) ->
            Word = sample(),
            Upper = Word bsr 5,
            Lower = Word band 31,
            [Host, separator, integer_to_list(Upper),
                   separator, integer_to_list(Lower),
                   separator, atom_to_list(node())].

        control(Host) ->
            Word = sample(),
            Upper = Word bsr 5,
            Lower = Word band 31,
            Left = integer_to_list(Upper),
            erlang:put(intervening, true),
            Right = integer_to_list(Lower),
            [Host, separator, Left, separator, Right, separator, atom_to_list(node())].

        sample() -> erlang:put(observed, visited).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] optimized = Body("render");
        object[] arithmetic = optimized.Where(instruction => Opcode(instruction) == GcBifTwo).ToArray();
        if (arithmetic.Length != 2
            || Value(Operands(arithmetic[0])[1]) != 1 || Kind(Operands(arithmetic[0])[3]) != BeamBinaryHostNames.X
            || Value(Operands(arithmetic[0])[3]) != 0 || Value(Operands(arithmetic[0])[^1]) != 1
            || Value(Operands(arithmetic[1])[1]) != 2 || Kind(Operands(arithmetic[1])[3]) != BeamBinaryHostNames.X
            || Value(Operands(arithmetic[1])[3]) != 0 || Kind(Operands(arithmetic[1])[^1]) != BeamBinaryHostNames.Y)
        {
            throw new InvalidOperationException(LostRoots);
        }

        object[] exchanges = optimized.Where(instruction => Opcode(instruction) == Swap).ToArray();
        if (exchanges is not [var exchange]
            || Operands(exchange) is not [var stack, var register]
            || Kind(stack) != BeamBinaryHostNames.Y || Kind(register) != BeamBinaryHostNames.X || Value(register) != 0
            || Body("control").Where(instruction => Opcode(instruction) == Swap)
                .Any(instruction => Kind(Operands(instruction)[0]) == BeamBinaryHostNames.Y))
        {
            throw new InvalidOperationException(IncorrectExchange);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static string Kind(object operand) => Property(operand, "Kind").ToString()!;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
