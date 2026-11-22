using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomCallEqualityChecks
{
    internal const string TestName = "compiler/beam/carries-source-proven-local-atom-results-to-exact-tests";
    private const string Fixture = "beam_atom_call_equality.erl";
    private const string Unexpected = "Only a local call whose returning paths yield atoms can type the comparison.";

    private const string Source = """
        -module(beam_atom_call_equality).
        -export([known/1, custom/1, boolean_branch/1, protected/1, open_protected/1, selected/1]).

        known(Selector) ->
            case flags(Selector) of yes -> 1; _ -> 0 end.

        flags([]) -> no;
        flags([take | _]) -> yes;
        flags([_ | Rest]) -> flags(Rest).

        custom(Value) ->
            case process_value(Value) of yes -> 1; _ -> 0 end.

        process_value(Value) -> erlang:put(fixture_key, Value).

        boolean_branch(Selector) ->
            case boolean_result(Selector) of true -> 1; _ -> 0 end.

        boolean_result([]) -> false;
        boolean_result([take | _]) -> true;
        boolean_result([_ | Rest]) -> boolean_result(Rest).

        protected(Input) ->
            case protected_result(Input) of ready -> 1; _ -> 0 end.
        protected_result(Input) ->
            try ready_or_throw(Input)
            catch throw:Reason -> {failed,Reason}
            end.
        ready_or_throw(ready) -> ready;
        ready_or_throw(Input) -> throw(Input).

        open_protected(Input) ->
            case open_result(Input) of ready -> 1; _ -> 0 end.
        open_result(Input) ->
            try ready_or_throw(Input)
            catch throw:Reason -> Reason
            end.

        selected(Selector) ->
            case color(Selector) of yellow -> 1; blue -> 2; green -> 3 end.
        color([]) -> yellow;
        color([blue | _]) -> blue;
        color([green | _]) -> green;
        color([_ | Rest]) -> color(Rest).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        if (!HasAtomResultTest("known") || HasAtomResultTest("custom") || HasAtomResultTest("boolean_branch")
            || !HasAtomResultTest("protected") || HasAtomResultTest("open_protected")
            || !HasAtomResultTest("selected", SelectValue, operand: 0))
        {
            throw new InvalidOperationException(Unexpected);
        }

        return Task.CompletedTask;

        bool HasAtomResultTest(string name, byte opcode = ExactEqualTest, int operand = 1)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").Skip(1).First(), "Value")] == name
            );
            object? check = info < 0 ? null : instructions.Skip(info + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .FirstOrDefault(instruction => Opcode(instruction) == opcode);

            return check is not null && Property(Items(check, "Operands").ElementAt(operand), "TypeIndex") is int;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
