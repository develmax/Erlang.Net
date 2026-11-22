using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleResultAtomFactChecks
{
    internal const string TestName = "compiler/beam/proves-atom-selectors-from-pair-members-and-record-arguments";
    private const string Fixture = "beam_tuple_result_atom_facts.erl";
    private const string WrongType = "A private atom selector did not preserve its source argument type.";

    private const string Source = """
        -module(beam_tuple_result_atom_facts).
        -export([known/1, unknown/1, field/1]).
        -record(result, {value}).

        known(Chars) ->
            {Tag, Rest} = atom_pair(Chars),
            {choose(Tag), Rest}.

        unknown(Value) ->
            {Tag, Rest} = unknown_pair(Value),
            {choose_any(Tag), Rest}.

        field(Chars) ->
            #result{value=choose_field(list_to_atom(Chars))}.

        atom_pair(Chars) -> {list_to_atom(Chars), rest}.
        unknown_pair(Value) -> {Value, rest}.

        choose(alpha) -> 4;
        choose(beta) -> 7;
        choose(gamma) -> 19;
        choose(_) -> other.

        choose_any(alpha) -> 4;
        choose_any(beta) -> 7;
        choose_any(gamma) -> 19;
        choose_any(_) -> other.

        choose_field(alpha) -> 4;
        choose_field(beta) -> 7;
        choose_field(gamma) -> 19;
        choose_field(_) -> other.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        if (!HasTypedSelector("choose") || !HasTypedSelector("choose_field") || HasTypedSelector("choose_any"))
        {
            throw new InvalidOperationException(WrongType);
        }

        return Task.CompletedTask;

        bool HasTypedSelector(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").Skip(1).First(), "Value")] == name
            );
            object? select = info < 0 ? null : instructions.Skip(info + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .FirstOrDefault(instruction => Opcode(instruction) == SelectValue);

            return select is not null && Property(Items(select, "Operands").First(), "TypeIndex") is int;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
