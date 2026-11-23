using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleListAppendLoopChecks
{
    internal const string TestName = "compiler/beam/reuses-checked-tuple-head-before-list-append";
    private const string ShapeError = "The append loop extracted its checked pair twice.";
    private const string Source = """
        -module(beam_tuple_list_append_loop).
        -export([render/2]).

        render([], Accumulator) ->
            lists:flatten(Accumulator);
        render([{[],[]}|Rest], Accumulator) ->
            render(Rest, Accumulator);
        render([{Left,Right}|Rest], Accumulator) ->
            render(Rest, [Left ++ ":" ++ Right ++ "]"|Accumulator]).
        """;

    internal static void Run()
    {
        string multiple = Source.Replace("\":\"", "\"::\"", StringComparison.Ordinal);
        if (multiple == Source || !SharesPair(Source) || SharesPair(multiple))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool SharesPair(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_tuple_list_append_loop.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();

        return instructions.Count(instruction => Opcode(instruction) == IsTuple) == 1
            && instructions.Count(instruction => Opcode(instruction) == TestArity) == 1
            && instructions.Count(instruction => Opcode(instruction) == GetTupleElement) == 2
            && instructions.Any(instruction => Opcode(instruction) == Allocate && Operands(instruction).SequenceEqual([3, 4]))
            && instructions.Any(instruction => Opcode(instruction) == TrimFrame && Operands(instruction).SequenceEqual([1, 2]));
    }

    private static int[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
        .Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
