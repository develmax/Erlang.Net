using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamNestedRecordBranchChecks
{
    internal static void Check()
    {
        const string source = """
            -module(beam_nested_record_branch).
            -export([handle/2]).
            -record(event, {payload, code}).
            -record(change, {kind, payload}).
            -record(state, {handle, config}).
            handle(#event{payload = #change{kind = page}}, State = #state{handle = Handle}) ->
                First = branch_dep:first(Handle),
                Second = branch_dep:second(Handle, First),
                branch_dep:log(State#state.config, "matched", [Second]),
                {noreply, State};
            handle(Event = #event{}, State = #state{}) ->
                branch_dep:log(State#state.config, "unmatched", [Event]),
                {noreply, State}.
            """;
        byte[] image = CodeGeneration.CompileBeamModule(source, "beam_nested_record_branch.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(image).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> primaryFrame = [BeamEmissionFormat.Allocate, 0x20, 0x20];
        ReadOnlySpan<byte> fallbackFrame = [BeamEmissionFormat.AllocateHeap, 0x10, 0x20, 0x20];
        ReadOnlySpan<byte> stackTrim = [BeamEmissionFormat.TrimFrame, 0x10, 0x10];
        ReadOnlySpan<byte> commonFrame = [BeamEmissionFormat.Allocate, 0x30, 0x20];
        if (code.AsSpan().IndexOf(primaryFrame) < 0 || code.AsSpan().IndexOf(fallbackFrame) < 0
            || code.AsSpan().IndexOf(stackTrim) < 0 || code.AsSpan().IndexOf(commonFrame) >= 0)
        {
            throw new InvalidOperationException(BeamNestedRecordBranchDiagnostics.BranchFrames);
        }

        _ = BeamModule.Prepare(image);
    }
}
