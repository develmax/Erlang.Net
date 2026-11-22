using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOctetEntrySelectorChecks
{
    internal const string TestName = "compiler/beam/types-private-selectors-from-unsigned-octet-matches";
    private const string Fixture = "beam_octet_entry_selector.erl";
    private const string Unexpected = "Expected only independently bounded unsigned byte selectors to carry the octet hint.";

    private const string Source = """
        -module(beam_octet_entry_selector).
        -export([unsigned_input/1, signed_input/1, mixed_input/1, record_input/1, record_update/2]).
        -record(frame, {kind}).

        unsigned_input(<<Key>>) -> choose(Key).
        signed_input(<<Key:8/signed>>) -> signed_choose(Key).
        mixed_input(<<Key>>) -> mixed_choose(Key);
        mixed_input(Value) -> mixed_choose(Value).
        record_input(<<Key>>) -> #frame{kind=record_choose(Key)}.
        record_update(<<Key>>, Existing) -> Existing#frame{kind=record_choose(Key)}.

        choose(0) -> first;
        choose(7) -> second;
        choose(250) -> third;
        choose(_) -> other.
        signed_choose(0) -> first;
        signed_choose(7) -> second;
        signed_choose(250) -> third;
        signed_choose(_) -> other.
        mixed_choose(0) -> first;
        mixed_choose(7) -> second;
        mixed_choose(250) -> third;
        mixed_choose(_) -> other.
        record_choose(0) -> first;
        record_choose(7) -> second;
        record_choose(250) -> third;
        record_choose(_) -> other.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] types = Items(Call(Type("BeamTypeTableReader"), "Read", structure), "Entries").ToArray();
        if (!HasOctetType("choose") || !HasOctetType("record_choose")
            || HasOctetType("signed_choose") || HasOctetType("mixed_choose"))
        {
            throw new InvalidOperationException(Unexpected);
        }

        return Task.CompletedTask;

        bool HasOctetType(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").Skip(1).First(), "Value")] == name
            );
            object? select = info < 0 ? null : instructions.Skip(info + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .FirstOrDefault(instruction => Opcode(instruction) == SelectValue);

            return select is not null && Property(Items(select, "Operands").First(), "TypeIndex") is int type
                && Property(types[type], "Minimum") is long min && min == 0
                && Property(types[type], "Maximum") is long max && max == byte.MaxValue;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
