using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBoundListEffectsChecks
{
    internal const string TestName = "compiler/beam/proves-bound-list-inputs-before-effect-mappers";
    private const string Fixture = "bound_list_effects_probe.erl";
    private const string WrongMapper = "The known proper generator retained its invalid-list branch or the unknown generator lost it.";
    internal const string Source = """
        -module(bound_list_effects_probe).
        -export([run/3, open/2, emit/3]).
        run(Owner, A, B) ->
            Values0 = [produce(A), produce(B)],
            Values = Values0,
            [?MODULE:emit(Owner, Value, recorded) || Value <- Values],
            done.

        open(Owner, Values) ->
            [?MODULE:emit(Owner, Value, recorded) || Value <- Values],
            done.

        produce(fault) -> erlang:error(source_fault);
        produce(Value) -> {produced, Value}.

        emit(Owner, Value, Tag) ->
            erlang:put(effects, [{Owner, Value, Tag} | erlang:get(effects)]),
            unused.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] closed = Mapping("-run/3-lc");
        object[] open = Mapping("-open/2-lc");
        object[] lists = closed.Where(instruction => Opcode(instruction) == GetList).ToArray();
        if (closed.Any(instruction => Opcode(instruction) == IsNil) || !open.Any(instruction => Opcode(instruction) == IsNil)
            || lists is not [var projection] || Items(projection, "Operands").ToArray() is not [_, var head, _]
            || Value(head) != 1)
        {
            throw new InvalidOperationException(WrongMapper);
        }

        return Task.CompletedTask;

        object[] Mapping(string prefix)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))].StartsWith(prefix, StringComparison.Ordinal)
            );

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
