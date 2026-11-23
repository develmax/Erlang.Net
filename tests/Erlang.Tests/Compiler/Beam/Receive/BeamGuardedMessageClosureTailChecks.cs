using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedMessageClosureTailChecks
{
    internal const string TestName = "compiler/beam/shares-a-received-message-element-and-captures-the-current-message";
    private const string Fixture = "beam_guarded_message_closure_tail.erl";
    private const string WrongLayout = "Guarded message selection lost its shared element read or message root.";

    private const string Source = """
        -module(beam_guarded_message_closure_tail).
        -export([loop/1, different_field/1]).

        loop(State) ->
            receive
                Message when element(1, Message) == alpha; element(1, Message) == omega ->
                    Result = fixture_sink:submit(fun() -> fixture_sink:apply(Message, State) end),
                    Updated = fixture_sink:consume(Result, State),
                    loop(Updated)
            end.

        different_field(State) ->
            receive
                Message when element(1, Message) == alpha; element(2, Message) == omega ->
                    Result = fixture_sink:submit(fun() -> fixture_sink:apply(Message, State) end),
                    Updated = fixture_sink:consume(Result, State),
                    different_field(Updated)
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] loop = Operations("loop");
        byte[] opcodes = loop.Select(Opcode).ToArray();
        int heap = Array.IndexOf(opcodes, TestHeap);
        int remove = Array.IndexOf(opcodes, RemoveMessage);
        if (loop.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray() is not [1]
            || opcodes.Count(opcode => opcode == BifTwo) != 1
            || Array.IndexOf(opcodes, SelectValue) < 0 || heap < 0 || remove != heap + 1
            || opcodes[remove + 1] != MakeFun || !opcodes.Contains(TrimFrame)
            || Operations("different_field").Count(instruction => Opcode(instruction) == BifTwo) != 2)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object[] Operations(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").Skip(1).First())] == name
            );

            return info < 0 ? [] : instructions.Skip(info + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
