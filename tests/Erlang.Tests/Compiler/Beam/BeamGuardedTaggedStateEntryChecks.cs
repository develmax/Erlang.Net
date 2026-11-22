using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTaggedStateEntryChecks
{
    internal const string TestName = "compiler/beam/selects-guarded-state-atoms-in-one-tagged-arm";
    private const string Fixture = "beam_guarded_tagged_state_entry_probe.erl";
    private const string WrongLayout = "A checked role and its guarded state alternatives must share a single owned entry.";

    private const string Source = """
        -module(beam_guarded_tagged_state_entry_probe).
        -export([dispatch/4]).
        dispatch(internal, {banner, Message, _}, {State, client}, Data)
          when State =:= first; State =:= second; State =:= third; State =:= fourth ->
            case Data of
                false -> io:format("~s",[Message]);
                true -> ok
            end,
            unchanged;
        dispatch(internal, {peer, Message, _}, {first, client}, Data) -> {peer, Message, Data};
        dispatch(internal, {peer, Message, _}, {second, client}, Data) -> {peer, Message, Data};
        dispatch(internal, {other}, {first, client}, Data) -> {other, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        """;

    internal static Task Run()
    {
        if (!HasOwnedStateSelect(Source) || HasOwnedStateSelect(Source.Replace(
            "case Data of",
            "case State of",
            StringComparison.Ordinal
        )) || HasOwnedStateSelect(Source.Replace(
            "State =:= fourth",
            "is_atom(State)",
            StringComparison.Ordinal
        )))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static bool HasOwnedStateSelect(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] prefix = [IsTuple, TestArity, GetTupleElement, ExactEqualTest, GetTupleElement, SelectValue];
        for (int start = 0; start + prefix.Length < instructions.Length; start++)
        {
            if (!instructions.Skip(start).Take(prefix.Length).Select(Opcode).SequenceEqual(prefix))
            {
                continue;
            }

            object[] selector = Operands(instructions[start + 5]);
            if (selector.Length != 11 || Value(selector[0]) != 4
                || Value(Operands(instructions[start + 2])[0]) != 2
                || Value(Operands(instructions[start + 2])[1]) != 1
                || Value(Operands(instructions[start + 4])[1]) != 0)
            {
                continue;
            }

            var keys = selector.Skip(3).Where((_, index) => index % 2 == 0).Select(operand => atoms[Value(operand)]);
            if (keys.SequenceEqual(["first", "fourth", "second", "third"])
                && selector.Skip(4).Where((_, index) => index % 2 == 0).Select(Value).Distinct().Count() == 1)
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
