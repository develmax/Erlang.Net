using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingletonRecordTailChecks
{
    internal const string TestName = "compiler/beam/singleton-record-tail-reuses-list-and-constructor-registers";
    private const string ScheduleMismatch = "The singleton record tail has a different projection or outgoing argument schedule.";
    private const string ControlMismatch = "The singleton record planner changed a branch with a retained selector result or an early projection.";
    private const string Source = """
        -module(singleton_record_tail).
        -export([four/4,permuted/3,one/3,alias_fallback/3]).

        four(A,B,C,D) ->
            case fixture_record_dep:select(A,{C}) of
                {selected,[{item,{data,V,W,X,Y}}]} -> fixture_record_dep:send(A,{data,V,W,X,Y,B,C},D);
                _ -> fixture_record_dep:send(A,{data,0,0,0,0,B,C},D)
            end.

        permuted(A,B,C) ->
            case fixture_record_dep:select(A,2) of
                {selected,[{item,{data,V,W}}]} -> fixture_record_dep:send(A,{data,W,V,B},C);
                _ -> fixture_record_dep:send(A,{data,0,0,B},C)
            end.

        one(A,B,C) ->
            case fixture_record_dep:select(A,1) of
                {selected,[{item,{data,V}}]} -> fixture_record_dep:send(A,{data,V,B},C);
                _ -> fixture_record_dep:send(A,{data,0,B},C)
            end.

        alias_fallback(A,B,C) ->
            case fixture_record_dep:select(A,2) of
                {selected,[{item,{data,V,W}}]} -> fixture_record_dep:send(A,{data,V,W,B},C);
                Other -> fixture_record_dep:send(A,{fallback,Other,B},C)
            end.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "singleton_record_tail.erl");
        object[] instructions = ((IEnumerable)Property(Code(image), "Instructions")).Cast<object>()
            .Where(instruction => Opcode(instruction) != Line).ToArray();
        int[] calls = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == ExternalCall).ToArray();
        if (calls.Length != 4)
        {
            throw new InvalidOperationException(ScheduleMismatch);
        }

        CheckMatch(calls[0], [0, 2, 3, 1], [0, 2, 3, 1]);
        CheckMatch(calls[1], [0, 1], [1, 0]);
        if (!Matches(
            calls[2] + 4,
            GetList,
            [0, 0, 1]
        )
            || !RetainsSelector(calls[3]))
        {
            throw new InvalidOperationException(ControlMismatch);
        }

        return Task.CompletedTask;

        bool RetainsSelector(int call)
        {
            int test = call + 1;
            int projection = call + 2;
            int listTest = call + 3;
            int split = call + 4;
            if (split >= instructions.Length || Opcode(instructions[test]) != IsTaggedTuple
                || Opcode(instructions[projection]) != GetTupleElement
                || Opcode(instructions[listTest]) != IsNonemptyList
                || Opcode(instructions[split]) != GetList)
            {
                return false;
            }

            object selector = ReadOperand(instructions[test], 1);
            object scratch = ReadOperand(instructions[projection], 2);
            BigInteger failure = Operand(instructions[test], 0);
            int fallback = Array.FindIndex(
                instructions,
                split + 1,
                instruction => Opcode(instruction) == Label && Operand(instruction, 0) == failure
            );
            int end = Array.FindIndex(instructions, split + 1, instruction => Opcode(instruction) == FunctionInfo);
            if (end < 0)
            {
                end = instructions.Length;
            }

            // The fallback must receive the original selector through every
            // failed check. The scratch projections must leave it in X0.
            return (string)Property(selector, "Kind") == "x"
                && (BigInteger)Property(selector, "Value") == 0
                && SameRegister(selector, ReadOperand(instructions[projection], 0))
                && !SameRegister(selector, scratch)
                && SameRegister(scratch, ReadOperand(instructions[listTest], 1))
                && SameRegister(scratch, ReadOperand(instructions[split], 0))
                && fallback > split && fallback + 2 < end
                && instructions[listTest..fallback].Where(instruction =>
                    Opcode(instruction) is IsTaggedTuple or IsNonemptyList or IsNil)
                    .All(instruction => Operand(instruction, 0) == failure)
                && Opcode(instructions[fallback + 1]) == TestHeap && Operand(instructions[fallback + 1], 1) >= 1
                && Opcode(instructions[fallback + 2]) == PutTuple && Operand(instructions[fallback + 2], 1) == 3
                && SameRegister(selector, ReadOperand(instructions[fallback + 2], 3));
        }

        void CheckMatch(int call, int[] projectedRegisters, int[] tupleRegisters)
        {
            int tuple = call + 10 + projectedRegisters.Length;
            int fallback = tuple + 6;
            if (!Matches(
                call + 4,
                GetList,
                [0, 1, 0]
            )
                || !Matches(
                    call + 6,
                    GetTupleElement,
                    [1, 1, 1]
                )
                || !Matches(
                    call + 9,
                    TestHeap,
                    [projectedRegisters.Length == 4 ? 8 : 5, 2]
                )
                || Opcode(instructions[tuple]) != PutTuple || Operand(instructions[tuple], 0) != 1
                || Opcode(instructions[fallback]) != PutTuple || Operand(instructions[fallback], 0) != 1
                || !Matches(
                    fallback - 1,
                    TestHeap,
                    [projectedRegisters.Length == 4 ? 8 : 5, 0]
                ))
            {
                throw new InvalidOperationException(ScheduleMismatch);
            }

            for (int index = 0; index < projectedRegisters.Length; index++)
            {
                if (!Matches(
                    call + 10 + index,
                    GetTupleElement,
                    [1, index + 1, projectedRegisters[index]]
                )
                    || Operand(instructions[tuple], index + 3) != tupleRegisters[index])
                {
                    throw new InvalidOperationException(ScheduleMismatch);
                }
            }

            foreach (int constructor in new[] { tuple, fallback })
            {
                if (!Matches(
                    constructor + 1,
                    Move,
                    [0, 2]
                )
                    || !Matches(
                        constructor + 2,
                        Move,
                        [projectedRegisters.Length == 4 ? 3 : 2, 0]
                    )
                    || Opcode(instructions[constructor + 3]) != TailExternalCallWithFrame)
                {
                    throw new InvalidOperationException(ScheduleMismatch);
                }
            }
        }

        bool Matches(int index, byte opcode, int[] operands)
        {
            return Opcode(instructions[index]) == opcode
                && operands.Select((operand, position) => Operand(instructions[index], position) == operand)
                    .All(matches => matches);
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(ReadOperand(instruction, index), "Value");

    private static object ReadOperand(object instruction, int index) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index);

    private static bool SameRegister(object left, object right) =>
        (string)Property(left, "Kind") == (string)Property(right, "Kind")
        && (BigInteger)Property(left, "Value") == (BigInteger)Property(right, "Value");
}
