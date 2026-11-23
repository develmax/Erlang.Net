using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedLookupChainChecks
{
    internal const string TestName = "compiler/beam/retains-case-selector-through-dependent-call-chain";
    private const string Fixture = "retained_lookup_chain_probe.erl";
    private const string WrongFrame = "The independent case result did not survive the lookup in its own trimmed slot.";
    private const string WrongDispatch = "A case with a guarded fallback lost its guard.";
    private const string Source = """
        -module(retained_lookup_chain_probe).
        -export([start/1, guarded/1]).
        start(Props) ->
            case proplists:get_value(selector, Props) of
                undefined -> {error, missing};
                Key ->
                    Value = proplists:get_value(value, Props, default),
                    Pair = pair(Key, Value),
                    erlang:put(result, Pair)
            end.

        guarded(Props) ->
            case proplists:get_value(selector, Props) of
                undefined -> {error, missing};
                Key when is_atom(Key) ->
                    Value = proplists:get_value(value, Props, default),
                    Pair = pair(Key, Value),
                    erlang:put(result, Pair);
                Other -> {error, Other}
            end.

        pair(Key, Value) -> {Key, Value}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] start = Body("start");
        object[] allocate = start.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        object[] trims = start.Where(instruction => Opcode(instruction) == TrimFrame).ToArray();
        int lookup = Array.FindLastIndex(start, instruction => Opcode(instruction) == ExternalCall);
        if (allocate is not [var frame] || !HasOperands(frame, 2, 1) || trims is not [var first, var second]
            || !HasOperands(first, 1, 1) || !HasOperands(second, 1, 0)
            || Array.IndexOf(start, first) >= lookup || Array.IndexOf(start, second) <= lookup)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] guarded = Body("guarded");
        if (!guarded.Any(instruction => Opcode(instruction) == IsAtom))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        return Task.CompletedTask;

        object[] Body(string function)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == function);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasOperands(object instruction, int first, int second) =>
        Operands(instruction) is [var a, var b] && Value(a) == first && Value(b) == second;

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static object[] Operands(object item) => Items(item, "Operands").ToArray();

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
