using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCommonCheckedTupleProjectionChecks
{
    internal const string TestName = "compiler/beam/shares-checked-tuple-fields-across-tag-dispatch";
    private const string Fixture = "beam_common_checked_projection_probe.erl";
    private const string WrongCommonField = "Each successful arm must consume the shared tuple field from before the tag dispatch.";
    private const string WrongDifferentFields = "Arms that read distinct fields must continue to read them separately.";

    internal const string Source = """
        -module(beam_common_checked_projection_probe).
        -export([run/3, bound/3, select/3, different/1, parse/1, effect/2]).
        run(Pair={tag,_}, 0, Input) ->
            case ?MODULE:parse(Input) of
                {accepted, Value} -> ?MODULE:effect(Pair, Value), ok;
                {error, Reason} -> erlang:error(Reason)
            end.
        bound(Pair={tag,Bound}, 0, Bound) ->
            case ?MODULE:parse(Bound) of
                {accepted, Value} -> ?MODULE:effect(Pair,Value), ok;
                {error, Reason} -> erlang:error(Reason)
            end.
        select(Pair={tag,_}, 0, Input) -> ?MODULE:effect(Pair, Input);
        select(_Pair, Flag, Input) -> ?MODULE:effect(Flag, Input).
        different(Input) ->
            case ?MODULE:parse(Input) of
                {alpha, First, _} -> ?MODULE:effect(alpha, First);
                {beta, _, Second} -> ?MODULE:effect(beta, Second)
            end.
        parse(Value) -> self() ! {parse,Value}, Value.
        effect(Key, Value) -> self() ! {effect,Key,Value}, {Key,Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] common = Function("run");
        int select = Array.FindIndex(common, instruction => Opcode(instruction) == SelectValue);
        var reads = common.Select((instruction, index) => (instruction, index))
            .Where(item => Opcode(item.instruction) == GetTupleElement
                && Value(Items(item.instruction, "Operands").ElementAt(1)) == 1).ToArray();
        if (select <= 0 || reads.Length != 1 || reads[0].index != select - 1)
        {
            throw new InvalidOperationException(WrongCommonField);
        }

        int[] different = Function("different").Where(instruction => Opcode(instruction) == GetTupleElement)
            .Select(instruction => Value(Items(instruction, "Operands").ElementAt(1))).ToArray();
        if (!different.Contains(1) || !different.Contains(2))
        {
            throw new InvalidOperationException(WrongDifferentFields);
        }

        object[] Function(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );
            if (info < 0)
            {
                throw new InvalidOperationException($"Missing fixture function {name}.");
            }

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
