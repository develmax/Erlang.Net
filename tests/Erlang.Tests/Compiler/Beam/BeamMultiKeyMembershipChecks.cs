using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMultiKeyMembershipChecks
{
    internal const string TestName = "compiler/beam/groups-consecutive-exact-atom-membership-tests";
    private const string Fixture = "beam_multi_key_membership_probe.erl";
    private const string SelectionMissing = "The compiler did not select among the atoms on one selector.";
    private const string MixedSelection = "Tests of different input registers were grouped as one selector.";

    private const string Source = """
        -module(beam_multi_key_membership_probe).
        -export([member/1, across/2, tag/1]).
        member(Attr) ->
            case Attr of
                {attribute, _, Kind, _} when Kind =:= cherry; Kind =:= apple; Kind =:= berry ->
                    true;
                _ ->
                    false
            end.

        across(A, B) when A =:= cherry; B =:= apple; A =:= berry -> match;
        across(_, _) -> reject.

        tag([{Tag, _} | Rest]) when Tag =:= cherry; Tag =:= apple; Tag =:= berry; Tag =:= date ->
            1 + tag(Rest);
        tag([_ | Rest]) ->
            tag(Rest);
        tag([]) ->
            0.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!Keys("member").SequenceEqual(["apple", "berry", "cherry"])
            || !Keys("tag").SequenceEqual(["apple", "berry", "cherry", "date"]))
        {
            throw new InvalidOperationException(SelectionMissing);
        }

        if (Keys("across").Length != 0)
        {
            throw new InvalidOperationException(MixedSelection);
        }

        return Task.CompletedTask;

        string[] Keys(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );
            object? select = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .FirstOrDefault(instruction => Opcode(instruction) == SelectValue);

            return select is null
                ? []
                : Items(select, "Operands").Skip(3).Where((operand, index) => index % 2 == 0)
                    .Select(operand => atoms[Value(operand)]).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
