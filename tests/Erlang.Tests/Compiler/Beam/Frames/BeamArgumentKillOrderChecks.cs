using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamArgumentKillOrderChecks
{
    private const string SurvivingArgumentMismatch = "The surviving first argument was not loaded from its remapped slot after trimming.";
    private const string ExpiringArgumentMismatch = "The expiring first argument was not loaded before its slot was reused.";

    internal static void Run()
    {
        const string source = """
            -module(beam_argument_kill_order).
            -export([survives/1,dies/1]).
            survives(Seed) ->
                Text = beam_spill_dep:text(Seed),
                Sizer = beam_spill_dep:sizer(),
                beam_spill_dep:observe(Text),
                beam_spill_dep:add(Sizer, Text),
                beam_spill_dep:attach(Sizer),
                {Sizer, Seed}.
            dies(Seed) ->
                Text = beam_spill_dep:text(Seed),
                Sizer = beam_spill_dep:sizer(),
                beam_spill_dep:observe(Text),
                beam_spill_dep:add(Text, Sizer),
                beam_spill_dep:attach(Sizer),
                {Sizer, Seed}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_argument_kill_order.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> survivesTrim =
        [
            BeamEmissionFormat.Move, 0x14, 0x13,
            BeamEmissionFormat.Move, 0x04, 0x14,
            BeamEmissionFormat.TrimFrame, 0x10, 0x20,
            BeamEmissionFormat.Move, 0x04, 0x03
        ];
        if (code.AsSpan().IndexOf(survivesTrim) < 0)
        {
            throw new InvalidOperationException(SurvivingArgumentMismatch);
        }

        ReadOnlySpan<byte> expiresBeforeTrim =
        [
            BeamEmissionFormat.Move, 0x04, 0x13,
            BeamEmissionFormat.Move, 0x14, 0x03,
            BeamEmissionFormat.Move, 0x04, 0x14,
            BeamEmissionFormat.TrimFrame, 0x10, 0x20
        ];
        if (code.AsSpan().IndexOf(expiresBeforeTrim) < 0)
        {
            throw new InvalidOperationException(ExpiringArgumentMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }
}
