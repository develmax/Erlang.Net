using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalRecordFieldChecks
{
    internal const string TestName = "compiler/beam/keeps-terminal-record-projections-and-closure-arguments-in-registers";
    private const string Fixture = "beam_terminal_record_fields.erl";
    private const string WrongRecord = "Terminal projections did not preserve the shared arity dispatch or direct rejection.";
    private const string WrongClosure = "The noncapturing closure clobbered the forwarded input or reserved an unnecessary stack slot.";

    private const string Source = """
        -module(beam_terminal_record_fields).
        -export([apply_children/2, rules/2, folded/1]).
        -record(tree, {children, label}).
        -record(text, {value}).
        -record(attribute, {name, value, flags}).

        apply_children(Fun, List) when is_list(List) ->
            lists:map(Fun, List);
        apply_children(Fun, Tree=#tree{}) ->
            lists:map(Fun, Tree#tree.children).

        rules(Fun, Tree=#tree{}) ->
            lists:map(Fun, Tree#tree.children);
        rules(_Fun, Text=#text{}) ->
            unicode:characters_to_list(Text#text.value);
        rules(_Fun, Attribute=#attribute{}) ->
            Attribute#attribute.value;
        rules(_Fun, _Other) ->
            [].

        folded(Value) ->
            lists:reverse(lists:foldl(fun collect/2, [], Value)).

        collect(Value, Acc) -> [Value|Acc].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] rules = Operations("rules");
        object[] apply = Operations("apply_children");
        if (rules.Count(instruction => Opcode(instruction) == SelectTupleArity) != 1
            || rules.Any(instruction => Opcode(instruction) is Allocate or BadRecord)
            || apply.Any(instruction => Opcode(instruction) == Jump)
            || apply.Count(instruction => Opcode(instruction) == IsTaggedTuple) != 1)
        {
            throw new InvalidOperationException(WrongRecord);
        }

        object[] folded = Operations("folded");
        object? frame = folded.FirstOrDefault(instruction => Opcode(instruction) == AllocateHeap);
        object? make = folded.FirstOrDefault(instruction => Opcode(instruction) == MakeFun);
        if (frame is null || Value(Operands(frame)[0]) != 0
            || make is null || Value(Operands(make)[1]) != 1
            || !folded.Any(instruction => Opcode(instruction) == TailExternalCallWithFrame && Value(Operands(instruction)[2]) == 0))
        {
            throw new InvalidOperationException(WrongClosure);
        }

        return Task.CompletedTask;

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(WrongRecord);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
