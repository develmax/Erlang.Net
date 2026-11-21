using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedReturningCallFrameChecks
{
    internal const string TestName = "compiler/beam/uses-zero-slot-frame-after-unframed-entry-checks";
    private const string Fixture = "checked_entry_returning_call_probe.erl";
    private const string WrongCheckedFrame = "The checked entry must reject before allocating the zero-slot call frame.";
    private const string LostSavedValue = "The guarded call result must retain the separate value used after the call.";
    private const string WrongColdTarget = "The cold clause must still be reachable through its own direct edge.";

    internal const string Source = """
        -module(checked_entry_returning_call_probe).
        -export([nested/1, checked_closure/1, retains/2, cold_result/1]).

        nested({{A,B,C},{D,E,F}}) ->
            lists:flatten(io_lib:format("~w-~w-~w/~w-~w-~w", [A,B,C,D,E,F])).

        checked_closure(Bytes) when is_list(Bytes) ->
            Encode = fun(C) -> C band 127 end,
            list_to_binary(lists:map(Encode, Bytes)).

        retains(Bytes, Head) when is_list(Bytes) ->
            Encode = fun(C) -> C band 127 end,
            Result = lists:map(Encode, Bytes),
            list_to_binary([Head | Result]).

        cold_result(Bytes) when is_list(Bytes) ->
            Encode = fun(C) -> C band 127 end,
            list_to_binary(lists:map(Encode, Bytes));
        cold_result(_) -> wrong_kind.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] starts = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo)
            .ToArray();
        CheckFrame(
            instructions,
            starts[0],
            starts[1],
            IsTuple
        );
        CheckFrame(
            instructions,
            starts[1],
            starts[2],
            IsList
        );

        object[] retained = instructions[starts[2]..starts[3]];
        int call = Array.FindIndex(retained, instruction => Opcode(instruction) == ExternalCall);
        if (call < 0 || !retained.Skip(call + 1).Any(instruction => Operands(instruction)
                .Any(operand => (string)Property(operand, "Kind") == "y")))
        {
            throw new InvalidOperationException(LostSavedValue);
        }

        object[] cold = instructions[starts[3]..starts[4]];
        var reject = cold.First(instruction => Opcode(instruction) == IsList);
        BigInteger target = Value(Operands(reject)[0]);
        if (target == InfoLabel(instructions, starts[3])
            || !cold.Any(instruction => Opcode(instruction) == Label && Value(Operands(instruction)[0]) == target))
        {
            throw new InvalidOperationException(WrongColdTarget);
        }

        return Task.CompletedTask;
    }

    private static void CheckFrame(
        object[] instructions,
        int start,
        int end,
        byte testOpcode
    )
    {
        object[] body = instructions[start..end];
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        int test = Array.FindIndex(body, instruction => Opcode(instruction) == testOpcode);
        object tail = body.Single(instruction => Opcode(instruction) == TailExternalCallWithFrame);
        if (test < 0 || frame <= test || Value(Operands(body[test])[0]) != InfoLabel(instructions, start)
            || Value(Operands(body[frame])[0]) != 0 || Value(Operands(tail)[^1]) != 0)
        {
            throw new InvalidOperationException(WrongCheckedFrame);
        }
    }

    private static BigInteger InfoLabel(object[] instructions, int start)
    {
        int label = start - 1;
        while (Opcode(instructions[label]) == Line)
        {
            label--;
        }

        return Value(Operands(instructions[label])[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");
}
