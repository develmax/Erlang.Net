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
            object saved = ReadOperand(instructions[call + 1], 1);
            object projected = ReadOperand(instructions[call + 3], 2);
            object input = ReadOperand(instructions[call + 5], 0);
            object? fallback = instructions[(call + 6)..]
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .LastOrDefault(instruction => Opcode(instruction) == PutTuple);

            // The general fallback needs the original selector even after the successful
            // branch reuses physical cells. Verify the value flow through both slots.
            return Opcode(instructions[call + 1]) == Move
                && (string)Property(ReadOperand(instructions[call + 1], 0), "Kind") == "x"
                && Operand(instructions[call + 1], 0).IsZero
                && Opcode(instructions[call + 3]) == GetTupleElement
                && SameStackRegister(saved, ReadOperand(instructions[call + 3], 0))
                && !SameStackRegister(saved, projected)
                && Opcode(instructions[call + 5]) == GetList
                && SameStackRegister(projected, input)
                && fallback is not null
                && SameStackRegister(saved, ReadOperand(fallback, 3));
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

    private static bool SameStackRegister(object left, object right) =>
        (string)Property(left, "Kind") == "y"
        && (string)Property(right, "Kind") == "y"
        && (BigInteger)Property(left, "Value") == (BigInteger)Property(right, "Value");
}
