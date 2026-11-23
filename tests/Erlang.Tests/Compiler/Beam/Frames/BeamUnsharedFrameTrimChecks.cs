using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamUnsharedFrameTrimChecks
{
    internal const string TestName = "compiler/beam/private-branch-trims-dead-stack-slots";
    private const string DeadSlotsMismatch = "The private branch kept dead stack slots across its next call.";
    private const string SharedFrameMismatch = "A branch with a shared continuation discarded its frame.";
    private const string LiveSlotMismatch = "A branch discarded a saved value needed after the call.";


    internal static Task Run()
    {
        const string source = """
            -module(beam_branch_zero_frame).
            -export([dynamic/2, shared/2, live/2, explicit/2]).
            dynamic(Target, Saved) ->
                case beam_trim_dep:select(Target) of
                    false -> beam_trim_dep:display({failed, Saved}), beam_trim_dep:stop();
                    true -> Target:keep(Saved)
                end.
            shared(Choice, Saved) ->
                case beam_trim_dep:select(Choice) of
                    false -> beam_trim_dep:display({failed, Saved});
                    true -> beam_trim_dep:display({selected, Saved})
                end,
                beam_trim_dep:stop().
            live(Choice, Saved) ->
                case beam_trim_dep:select(Choice) of
                    false -> beam_trim_dep:display({failed, Saved}), beam_trim_dep:keep(Saved);
                    true -> beam_trim_dep:keep(Saved)
                end.
            explicit(Key, Saved) ->
                Value = beam_trim_dep:make(Key),
                beam_trim_dep:mark(Value),
                beam_trim_dep:display({failed, Value}),
                beam_trim_dep:keep(Saved).
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_branch_zero_frame.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> exclusiveBranchTrim = [BeamEmissionFormat.TrimFrame, 0x20, 0x00];
        if (code.AsSpan().IndexOf(exclusiveBranchTrim) < 0)
        {
            throw new InvalidOperationException(DeadSlotsMismatch);
        }

        ReadOnlySpan<byte> sharedFrameTrim = [BeamEmissionFormat.TrimFrame, 0x10, 0x00];
        if (code.AsSpan().IndexOf(sharedFrameTrim) >= 0)
        {
            throw new InvalidOperationException(SharedFrameMismatch);
        }

        ReadOnlySpan<byte> partialTrim = [BeamEmissionFormat.TrimFrame, 0x10, 0x10];
        if (code.AsSpan().IndexOf(partialTrim) < 0)
        {
            throw new InvalidOperationException(LiveSlotMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
