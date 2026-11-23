using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamClosureFreeSpillChecks
{
    private const string LiveSlotMismatch = "The saved call result did not reuse the slot released by its last argument read.";
    private const string RetainedSlotMismatch = "A value read after the next call lost its separate frame slot.";

    internal static void Run()
    {
        const string source = """
            -module(beam_closure_free_spill).
            -export([value/1,keep/1]).
            value(Seed) ->
                Text = beam_spill_dep:text(Seed),
                beam_spill_dep:observe(Text),
                Sizer = beam_spill_dep:sizer(),
                beam_spill_dep:add(Sizer, Text),
                beam_spill_dep:attach(Sizer),
                {Sizer, Seed}.
            keep(Seed) ->
                Text = beam_spill_dep:text(Seed),
                beam_spill_dep:observe(Text),
                Sizer = beam_spill_dep:sizer(),
                beam_spill_dep:add(Sizer, Text),
                beam_spill_dep:attach(Sizer),
                {Sizer, Seed, Text}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_closure_free_spill.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> compactAllocation = [BeamEmissionFormat.Allocate, 0x20, 0x10];
        ReadOnlySpan<byte> readBeforeSpill = [BeamEmissionFormat.Move, 0x04, 0x13, BeamEmissionFormat.Move, 0x03, 0x04];
        if (code.AsSpan().IndexOf(compactAllocation) < 0 || code.AsSpan().IndexOf(readBeforeSpill) < 0)
        {
            throw new InvalidOperationException(LiveSlotMismatch);
        }

        ReadOnlySpan<byte> separateAllocation = [BeamEmissionFormat.Allocate, 0x30, 0x10];
        if (code.AsSpan().IndexOf(separateAllocation) < 0)
        {
            throw new InvalidOperationException(RetainedSlotMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }
}
