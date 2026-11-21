using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalSingletonListCaseChecks
{
    internal const string TestName = "compiler/beam/uses-list-head-dispatch-and-terminal-empty-list-branches";
    private const string Fixture = "beam_terminal_singleton_list_case.erl";
    private const string WrongDispatch = "The head dispatch lost exact singleton-list matching or the local tail.";
    private const string WrongEmpty = "The empty-list arm did not preserve the original fallback result and tail frame.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_terminal_singleton_list_case).
        -export([menu/2, ask/2, value_menu/2, empty_retry/2, unsupported/2]).

        menu(Key, Fallback) ->
            audit(Key),
            case erlang:get(Key) of
                "y" -> accepted;
                "n" -> rejected;
                "Y" -> accepted;
                "N" -> rejected;
                _ -> value(Fallback)
            end.

        ask(Key, Fallback) ->
            audit(Key),
            case erlang:get(Key) of
                "" -> value(Fallback);
                Present -> Present
            end.

        value_menu(Key, Fallback) ->
            audit(Key),
            case erlang:get(Key) of
                "A" -> first;
                "Z" -> second;
                "B" -> first;
                "a" -> second;
                _ -> value(Fallback)
            end.

        empty_retry(Key, Fallback) ->
            audit(Key),
            case erlang:get(Key) of
                [] -> value(Fallback);
                Present -> Present
            end.

        unsupported(Key, Fallback) ->
            case erlang:get(Key) of
                [X] when is_integer(X) -> X;
                [] -> default;
                _ -> value(Fallback)
            end.

        value(Key) -> erlang:get(Key).
        audit(Key) -> erlang:put(observed, Key).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        CheckDispatch("menu");
        CheckDispatch("value_menu");
        CheckEmpty("ask");
        CheckEmpty("empty_retry");

        object[] guarded = Operations("unsupported");
        if (guarded.Any(instruction => Opcode(instruction) == SelectValue)
            || !guarded.Any(instruction => Opcode(instruction) == IsInteger))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        return Task.CompletedTask;

        void CheckDispatch(string name)
        {
            object[] ops = Operations(name);
            if (ops.Count(instruction => Opcode(instruction) == SelectValue) != 1
                || ops.Count(instruction => Opcode(instruction) == IsNil) != 2
                || ops.Count(instruction => Opcode(instruction) == GetList) != 1
                || ops.Count(instruction => Opcode(instruction) == IsNonemptyList) != 1
                || !ops.Any(instruction => Opcode(instruction) == TailLocalCallWithFrame))
            {
                throw new InvalidOperationException(WrongDispatch);
            }
        }

        void CheckEmpty(string name)
        {
            object[] ops = Operations(name);
            if (ops.Count(instruction => Opcode(instruction) == IsNil) != 1
                || ops.Any(instruction => Opcode(instruction) == ExactEqualTest)
                || !ops.Any(instruction => Opcode(instruction) == TailLocalCallWithFrame))
            {
                throw new InvalidOperationException(WrongEmpty);
            }
        }

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(Missing);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
