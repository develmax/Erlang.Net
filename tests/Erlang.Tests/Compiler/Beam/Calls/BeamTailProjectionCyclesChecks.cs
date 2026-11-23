using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTailProjectionCyclesChecks
{
    internal const string TestName = "compiler/beam/orders-checked-projections-before-tail-argument-cycles";
    private const string SourceFile = "beam_tail_projection_cycles_probe.erl";
    private const string IncorrectMoves = "The terminal argument registers did not follow the checked projection order.";

    private const string Source = """
        -module(beam_tail_projection_cycles_probe).
        -export([encode/3, decode/2, keep/4]).
        encode(Data, {Id, Meta}, Secret) ->
            ?MODULE:keep(Data, Secret, Id, Meta).
        decode({_Tag, Data, {Id, Meta}}, Secret) ->
            ?MODULE:keep(Data, Secret, Id, Meta).
        keep(Data, Secret, Id, Meta) -> {Data, Secret, Id, Meta}.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, SourceFile);
        object[] instructions = ((IEnumerable)Property(Code(image), "Instructions")).Cast<object>().ToArray();
        var swaps = instructions.Where(instruction => Opcode(instruction) == Swap)
            .Select(instruction => ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
                .Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray()).ToArray();
        if (swaps.Length != 3 || !swaps[0].SequenceEqual(new[] { 3, 2 })
            || !swaps[1].SequenceEqual(new[] { 1, 3 }) || !swaps[2].SequenceEqual(new[] { 2, 3 }))
        {
            throw new InvalidOperationException(IncorrectMoves);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
