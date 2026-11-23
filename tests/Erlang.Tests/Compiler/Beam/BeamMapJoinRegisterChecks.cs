using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMapJoinRegisterChecks
{
    internal const string TestName = "compiler/beam/keeps-the-original-map-register-across-constructor-joins";
    private const string Failure = "The formal map must remain in X0 until it is returned or passed to the iterator.";
    private const string Source = """
        -module(map_join_register_probe).
        -export([adapt/1]).

        adapt(Input) ->
            Selection =
                case Input of
                    #{left := Left, right := none} ->
                        {left_value, Left};
                    #{left := none, right := Right} ->
                        {right_value, Right};
                    #{left := Left, right := Right} ->
                        {pairs, #{left_choice => Left, right_choice => Right}}
                end,
            case Input of
                #{entry := {packed, Data}} when is_list(Data); is_binary(Data) ->
                    Base = #{data => Data, selection => Selection},
                    case Input of
                        #{note := none} -> Base;
                        #{note := Extra} -> Base#{note => Extra}
                    end;
                _ ->
                    #{K => V || K := V <- Input, V =/= none}
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "map_join_register_probe.erl"));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int header = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == "adapt"
        );
        if (header < 0 || Opcode(instructions[header + 2]) != IsMap
            || Value(Items(instructions[header + 2], "Operands").ElementAt(1)) != 0
            || Opcode(instructions[header + 3]) != GetMapElements)
        {
            throw new InvalidOperationException(Failure);
        }

        object[] projection = Items(instructions[header + 3], "Operands").ToArray();
        if (projection.Length != 7 || Value(projection[1]) != 0 || Value(projection[4]) != 2 || Value(projection[6]) != 1)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
