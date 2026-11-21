using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedEntryClosureOrderChecks
{
    internal const string TestName = "compiler/beam/owned-atom-entry-retains-lifted-capture-order";
    private const string WrongCaptureOrder = "The lifted callback did not receive the captured arguments in OTP order.";
    private const string Source = """
        -module(beam_owned_closure).
        -export([capture/4]).
        -record(state,{context,value}).
        capture(sync, From, Category, S=#state{}) ->
            {sync,From,Category,S};
        capture(async, From, Category, S=#state{}) ->
            fun() ->
                service:mark(),
                service:write(From, Category, S)
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_owned_closure.erl"));
        var instructions = Items(code, "Instructions");
        object lifted = Items(code, "Functions").Single(function =>
            (string)Property(function, "Name") == "-capture/4-fun-0-");
        int start = (int)Property(lifted, "FirstInstructionIndex");
        int end = (int)Property(lifted, "EndInstructionIndex");
        var outgoingMoves = instructions[start..end].SkipWhile(instruction => Opcode(instruction) != ExternalCall)
            .Skip(1).Where(instruction => Opcode(instruction) == Move)
            .Select(instruction => Items(instruction, "Operands").Select(Operand).ToArray()).ToArray();
        if (outgoingMoves is not [[("y", 0), ("x", 2)], [("y", 1), ("x", 1)], [("y", 2), ("x", 0)]])
        {
            throw new InvalidOperationException(WrongCaptureOrder);
        }
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static (string Kind, int Value) Operand(object operand) =>
        ((string)Property(operand, "Kind"), (int)(BigInteger)Property(operand, "Value"));
}
