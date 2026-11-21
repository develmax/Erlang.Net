using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingletonGeneratorHeadChecks
{
    internal const string TestName = "compiler/beam/separates-singleton-generator-head-and-source-failure";
    private const string Fixture = "beam_singleton_generator_head_probe.erl";
    private const string InvalidGeneratorEntry = "The generator must preserve distinct rejected source and skipped head edges.";
    private const string LostAllocation = "Only the successful singleton arm owns its recursive return frame.";

    internal const string Source = """
        -module(beam_singleton_generator_head_probe).
        -export([one/1, captured/2, strict/1, two/1, eager/1, triple/2, quad/1]).

        one(Input) -> [H || [H] <- Input].
        captured(Input, Tag) -> [{Tag,H} || [H] <- Input].
        strict(Input) -> [H || [H] <:- Input].
        two(Input) -> [{A,B} || [A,B] <- Input].
        eager(Input) -> [H || [H] <- Input, is_integer(H)].
        triple(Input, Tag) -> [{Tag,A,B,C} || [A,B,C] <- Input].
        quad(Input) -> [{A,B,C,D} || [A,B,C,D] <- Input].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        CheckEntry("one", 1, 1);
        CheckEntry("captured", 2, 1);
        CheckEntry("two", 1, 2);
        CheckEntry("triple", 2, 3);
        CheckEntry("quad", 1, 4);

        return Task.CompletedTask;

        void CheckEntry(string parent, int headRegister, int cells)
        {
            int function = Array.FindIndex(functions, candidate =>
                ((string)Property(candidate, "Name")).StartsWith($"-{parent}/", StringComparison.Ordinal));
            int first = (int)Property(functions[function], "FirstInstructionIndex");
            int last = function + 1 < functions.Length
                ? (int)Property(functions[function + 1], "FirstInstructionIndex") : instructions.Length;
            object[] entry = instructions[first..last];
            int checks = Array.FindIndex(entry, instruction => Opcode(instruction) == IsNonemptyList);
            int allocation = Array.FindIndex(entry, instruction => Opcode(instruction) is Allocate or AllocateHeap);
            int nilIndex = checks + 2 + cells * 2;
            if (checks < 0 || allocation <= nilIndex)
            {
                throw new InvalidOperationException(LostAllocation);
            }

            object outer = entry[checks];
            object pair = entry[checks + 1];
            object head = entry[checks + 2];
            object nil = entry[nilIndex];
            if (Opcode(pair) != GetList || Opcode(head) != IsNonemptyList || Opcode(nil) != IsNil
                || !IsX(pair, 0, 0) || !IsX(pair, 1, headRegister) || !IsX(pair, 2, 0)
                || !IsX(nil, 1, headRegister) || Value(outer, 0) == Value(head, 0) || Value(head, 0) != Value(nil, 0))
            {
                throw new InvalidOperationException(InvalidGeneratorEntry);
            }

            for (int cell = 0; cell < cells; cell++)
            {
                object check = entry[checks + 2 + cell * 2];
                object read = entry[checks + 3 + cell * 2];
                if (Opcode(check) != IsNonemptyList || Opcode(read) != GetList
                    || !IsX(check, 1, headRegister) || Value(check, 0) != Value(head, 0)
                    || !IsX(read, 0, headRegister) || !IsX(read, 1, headRegister + cell + 1)
                    || !IsX(read, 2, headRegister))
                {
                    throw new InvalidOperationException(InvalidGeneratorEntry);
                }
            }

            int skip = Array.FindIndex(entry, instruction =>
                Opcode(instruction) == Label && Value(instruction, 0) == Value(head, 0));
            if (skip <= allocation || Opcode(entry[skip + 1]) != TailLocalCall)
            {
                throw new InvalidOperationException(InvalidGeneratorEntry);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object Operand(object instruction, int index) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index);

    private static BigInteger Value(object instruction, int index) => (BigInteger)Property(Operand(instruction, index), "Value");

    private static bool IsX(
        object instruction,
        int index,
        int register
    ) => (string)Property(Operand(instruction, index), "Kind") == "x" && Value(instruction, index) == register;
}
