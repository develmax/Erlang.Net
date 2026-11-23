using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStackArgumentLifetimeChecks
{
    internal const string TestName = "compiler/beam/reuses-dead-outgoing-stack-values-without-clobbering-live-inputs";
    private const string Source = """
        -module(ordered_stack_value_probe).
        -export([closed/1, live/1, reserve/1, tap/2, record/4, finish/3]).
        closed(Input) ->
            A = ?MODULE:reserve({a, Input}),
            B = ?MODULE:reserve({b, Input}),
            C = ?MODULE:reserve({c, Input}),
            Tail = ?MODULE:reserve({tail, Input}),
            ?MODULE:record(A, B, C, [{value, Tail}]),
            Left = ?MODULE:tap(Input, left),
            Right = ?MODULE:tap(Input, right),
            ?MODULE:finish(Input, Left, Right).
        live(Input) ->
            A = ?MODULE:reserve({a, Input}),
            B = ?MODULE:reserve({b, Input}),
            C = ?MODULE:reserve({c, Input}),
            Tail = ?MODULE:reserve({tail, Input}),
            ?MODULE:record(A, B, C, [{value, Tail}]),
            Left = ?MODULE:tap(Input, left),
            Right = ?MODULE:tap(Input, right),
            ?MODULE:finish(A, Left, Right).
        reserve({_, fail}) -> erlang:error(known_error);
        reserve(Value) -> {reserved, Value}.
        tap(Input, Name) -> {Input, Name}.
        record(A,B,C,D) -> put(trace, {A,B,C,D}).
        finish(Input, A, B) -> {Input, A, B, erase(trace)}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "ordered_stack_value_probe.erl"));
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] closed = Setup("closed");
        object[] live = Setup("live");
        Require(closed.Length == 3 && closed.All(instruction => Opcode(instruction) == Move));
        Require(IsRegister(Items(closed[0], "Operands")[1], "y", 0));
        Require(IsRegister(Items(closed[1], "Operands")[0], "y", 1));
        Require(IsRegister(Items(closed[1], "Operands")[1], "x", 0));
        Require(IsRegister(Items(closed[2], "Operands")[1], "x", 1));
        Require(live.Length == 2 && Opcode(live[0]) == Swap && Opcode(live[1]) == Move);

        System.Type compiler = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        Require(KilledValues(compiler, overwriteFirstArgument: true).SequenceEqual([2, 1, 0]));
        Require(KilledValues(compiler, overwriteFirstArgument: false).Length == 0);

        return Task.CompletedTask;

        object[] Setup(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands")[1])] == name);

            return instructions.Skip(entry + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .SkipWhile(instruction => Opcode(instruction) != ExternalCall || Value(Items(instruction, "Operands")[0]) != 4)
                .Skip(1)
                .SkipWhile(instruction => Opcode(instruction) != ExternalCall || Value(Items(instruction, "Operands")[0]) != 2)
                .Skip(1)
                .TakeWhile(instruction => Opcode(instruction) is Move or Swap)
                .ToArray();
        }
    }

    private static int[] KilledValues(System.Type compiler, bool overwriteFirstArgument)
    {
        object Call(int arity) => Instruction(ExternalCall, Op("unsigned", arity), Op("unsigned", 0));

        object MoveYToX(int from, int to) => Instruction(Move, Op("y", from), Op("x", to));

        object MoveXToY(int from, int to) => Instruction(Move, Op("x", from), Op("y", to));
        object[] instructions =
        [
            Call(1),
            Instruction(
                PutList,
                Op("x", 0),
                Op("nil", 0),
                Op("x", 3)
            ),
            MoveYToX(0, 2),
            MoveYToX(1, 1),
            MoveYToX(2, 0),
            Call(4),
            MoveXToY(0, 1),
            Call(1),
            overwriteFirstArgument ? MoveXToY(0, 2) : MoveYToX(2, 0),
            Instruction(Deallocate, Op("unsigned", 4))
        ];
        Array body = Array.CreateInstance(Type(BeamBinaryHostNames.Instruction), instructions.Length);
        instructions.CopyTo(body, 0);

        return (int[])BeamBinaryHostAccess.Call(
            compiler,
            "ReadOutgoingStackValueKills",
            body,
            0,
            5,
            9,
            instructions[5],
            false
        );
    }

    private static object Instruction(byte opcode, params object[] operands) => New(
        BeamBinaryHostNames.Instruction,
        0,
        New(
            BeamBinaryHostNames.Definition,
            opcode,
            "fixture",
            operands.Length,
            false
        ),
        Ops(operands)
    );

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static bool IsRegister(object operand, string kind, int index) =>
        (string)Property(operand, "Kind") == kind && Value(operand) == index;

    private static void Require(bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("An outgoing stack value was cleared or scheduled outside its lifetime.");
        }
    }
}
