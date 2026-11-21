using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDirectListDestinationChecks
{
    internal const string TestName = "compiler/beam/writes-lists-directly-to-dead-copy-destinations";
    private const string Fixture = "beam_direct_list_destination_probe.erl";
    private const string MissingDirectDestination = "A constructed list still passes through a dead argument register.";
    private const string LostSharedValue = "The list is no longer available in both live call arguments.";

    private const string Source = """
        -module(beam_direct_list_destination_probe).
        -export([wrap/1, share/1, choose/2, query/1]).
        wrap(Value) when is_binary(Value) -> Value;
        wrap(Value) -> fail(Value).

        fail(Value) ->
            erlang:error(badarg, [Value], [{error_info, #{module => erl_stdlib_errors}}]).

        share(Value) ->
            List = [Value],
            result(List, List).

        choose(Kind, Value) ->
            case Kind of
                selected -> result(Kind, [Value]);
                _ -> {ignored, Value}
            end.

        result(Kind, Args) ->
            {Kind, Args}.

        query(Pattern) ->
            Spec = [{Pattern, [], ['$$']}],
            ets:select(beam_list_destination_table, Spec).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] query = Body("query");
        int list = Array.FindIndex(query, instruction => Opcode(instruction) == PutList);
        if (list < 0 || Value(Operands(query[list])[^1]) != 1 || Copies(query[list + 1], 0, 1))
        {
            throw new InvalidOperationException(MissingDirectDestination);
        }

        object[] share = Body("share");
        list = Array.FindIndex(share, instruction => Opcode(instruction) == PutList);
        if (list < 0 || Value(Operands(share[list])[^1]) != 0 || !Copies(share[list + 1], 0, 1))
        {
            throw new InvalidOperationException(LostSharedValue);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool Copies(object instruction, int sourceNumber, int destinationNumber) =>
        Opcode(instruction) == Move && Operands(instruction) is [var source, var destination]
        && (string)Property(source, "Kind") == "x" && (string)Property(destination, "Kind") == "x"
        && Value(source) == sourceNumber && Value(destination) == destinationNumber;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
