using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDisjointNilIntegerEntriesChecks
{
    private const int IntegerUnion = 0x0020;

    internal static Task Run()
    {
        const string source = """
            -module(beam_nil_integer_entry_probe).
            -export([route/2, numeric/2]).
            route([], _) -> exit({error, {probe, empty}});
            route(49, Input) -> exit({error, {probe, Input}});
            route(Other, Input) -> exit({error, {probe, Other, Input}}).
            numeric([], _) -> exit({error, {probe, empty}});
            numeric(Number, Input) when Number == 49 -> exit({error, {probe, Input}});
            numeric(Other, Input) -> exit({error, {probe, Other, Input}}).
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_nil_integer_entry_probe.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> partitioned =
        [
            BeamEmissionFormat.IsInteger, 0x35, 0x03, BeamEmissionFormat.ExactEqualTest, 0x45,
            BeamEmissionFormat.ExtendedTypedRegisterOperand, 0x03
        ];
        ReadOnlySpan<byte> numeric =
        [
            BeamEmissionFormat.NumericEqualTest, 0x85, 0x03, 0x09, 0x31
        ];
        int selected = code.AsSpan().IndexOf(partitioned);
        if (selected < 0 || code.AsSpan(selected + partitioned.Length).IndexOf(numeric) < 0)
        {
            throw new InvalidOperationException("Only a literal integer pattern can bypass the disjoint nil arm.");
        }

        object decoded = Code(beam);
        object[] instructions = Items(decoded, "Instructions");
        object[] types = Items(Property(decoded, "Types"), "Entries");
        object equality = instructions.Single(instruction =>
            Property(Property(instruction, "Definition"), "Opcode") is BeamEmissionFormat.ExactEqualTest);
        if (Property(Items(equality, "Operands")[1], "TypeIndex") is not int type
            || Property(types[type], "TypeUnion") is not IntegerUnion
            || Property(types[type], "MetadataFlags") is not 0)
        {
            throw new InvalidOperationException("The integer test does not establish a finite range.");
        }

        return Task.CompletedTask;
    }

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
