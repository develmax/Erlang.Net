using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaughtFunctionContainerChecks
{
    internal const string TestName = "compiler/beam/builds-closed-function-containers-before-a-catch";
    private const string Fixture = "beam_caught_function_container_probe.erl";
    private const string WrongSetup = "Expected the declared closure and its container in the outgoing register before catch installation.";
    private const string EscapedEffect = "An effectful argument must remain inside the original catch.";

    internal const string Source = """
        -module(beam_caught_function_container_probe).
        -export([closed/1, effectful/1]).
        closed(Input) ->
            case catch beam_caught_function_peer:read(Input,
                    [{callback, fun callback/1}, {state, []}]) of
                {ok, Value, _} -> {ok, beam_caught_function_peer:second(beam_caught_function_peer:first(Value))};
                Bad -> {error, {failed, Bad}}
            end.
        effectful(Input) ->
            case catch beam_caught_function_peer:read(Input,
                    [{callback, fun callback/1}, {state, beam_caught_function_peer:state(Input)}]) of
                {ok, Value, _} -> Value;
                Bad -> {error, {failed, Bad}}
            end.
        callback(Input) -> {called, Input}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] closed = Body("closed").Where(operation => Opcode(operation) != Label).ToArray();
        if (closed.Length < 6 || closed.Take(5).Select(Opcode).ToArray()
                is not [AllocateHeap, MakeFun, PutTuple, PutList, CatchEnter]
            || Operands(closed[0]) is not [var width, _, var live] || Value(width) != 1 || Value(live) != 1
            || Operands(closed[1]) is not [_, var destination, var environment]
            || (string)Property(destination, "Kind") != "x" || Value(destination) != 1 || Value(environment) != 0)
        {
            throw new InvalidOperationException(WrongSetup);
        }

        object[] effectful = Body("effectful");
        int firstCall = Array.FindIndex(effectful, operation => Opcode(operation) == ExternalCall);
        int entry = Array.FindIndex(effectful, operation => Opcode(operation) == CatchEnter);
        int exit = Array.FindIndex(effectful, operation => Opcode(operation) == CatchEnd);
        if (entry < 0 || firstCall <= entry || firstCall >= exit)
        {
            throw new InvalidOperationException(EscapedEffect);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                operation => Opcode(operation) == FunctionInfo
                    && Operands(operation) is [_, var atom, _] && atoms[Value(atom)] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
