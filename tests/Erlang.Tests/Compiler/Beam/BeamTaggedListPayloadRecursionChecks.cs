using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedListPayloadRecursionChecks
{
    internal const string TestName = "compiler/beam/keeps-tagged-list-payload-through-recursion";
    private const string Fixture = "tagged_list_payload_recursion_probe.erl";
    private const string WrongFrames = "The pair and property-list branches did not own independent frames.";
    private const string Source = """
        -module(tagged_list_payload_recursion_probe).
        -export([build/2, control/2]).
        build([], Acc) -> Acc;
        build([{service, {Name, Value}} | Tail], Acc) ->
            Item = pair(Name, Value),
            build(Tail, [Item | Acc]);
        build([{service, Options} | Tail], Acc) when is_list(Options) ->
            Name = proplists:get_value(name, Options),
            Value = proplists:get_value(value, Options),
            Item = pair(Name, Value),
            build(Tail, [Item | Acc]).

        control([], Acc) -> Acc;
        control([{service, {Name, Value}} | Tail], Acc) ->
            Item = pair(Name, Value),
            control(Tail, [Item | Acc]);
        control([{other, Options} | Tail], Acc) when is_list(Options) ->
            Name = proplists:get_value(name, Options),
            Value = proplists:get_value(value, Options),
            Item = pair(Name, Value),
            control(Tail, [Item | Acc]).

        pair(Name, Value) -> {Name, Value}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] matched = Body("build");
        object[] differentTag = Body("control");
        int[] frameSizes = matched.Where(instruction => Opcode(instruction) == Allocate)
            .Select(instruction => Value(Operands(instruction)[0])).ToArray();
        object[] trims = matched.Where(instruction => Opcode(instruction) == TrimFrame).ToArray();
        if (frameSizes is not [2, 3] || trims is not [var trim]
            || Operands(trim) is not [var expired, var retained] || Value(expired) != 1 || Value(retained) != 2
            || matched.Count(instruction => Opcode(instruction) == IsTaggedTuple) != 1
            || differentTag.Count(instruction => Opcode(instruction) == IsTaggedTuple) != 2)
        {
            throw new InvalidOperationException(WrongFrames);
        }

        return Task.CompletedTask;

        object[] Body(string function)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == function);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static object[] Operands(object item) => Items(item, "Operands").ToArray();

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
