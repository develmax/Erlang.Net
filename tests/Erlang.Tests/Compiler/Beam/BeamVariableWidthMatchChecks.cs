using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamVariableWidthMatchChecks
{
    internal const string TestName = "compiler/beam/keeps-known-variable-width-match-context-in-x";
    private const string Fixture = "beam_variable_width_match_probe.erl";
    private const string SpilledContext = "The known integer match must retain its X context through the exhaustion test.";
    private const string Source = """
        -module(beam_variable_width_match_probe).
        -export([unpack/2, open/2]).

        unpack(Count, Width) ->
            <<Value:Width>> = <<1, 0:Count>>,
            {Width, Value}.

        open(Bytes, Width) ->
            <<Value:Width>> = Bytes,
            {Width, Value}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int first = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == "unpack"
        );
        object[] unpack = instructions.Skip(first + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int start = Array.FindIndex(unpack, instruction => Opcode(instruction) == StartBinaryMatchKnown);
        if (start < 0 || start + 2 >= unpack.Length
            || Opcode(unpack[start + 1]) != GetBinaryInteger || Opcode(unpack[start + 2]) != BinaryMatch
            || Value(unpack[start], 3) != 1 || Value(unpack[start + 1], 1) != 1 || Value(unpack[start + 2], 1) != 1)
        {
            throw new InvalidOperationException(SpilledContext);
        }
    }

    private static int Value(object instruction, int operand) =>
        (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(operand), "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
