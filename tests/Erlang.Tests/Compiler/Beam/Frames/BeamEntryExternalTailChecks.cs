using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryExternalTailChecks
{
    internal static void Run()
    {
        CheckTrappingTailAllocation();
        const string source = """
            -module(beam_entry_tail_list).
            -export([run/2, trap/1]).
            run([], Acc) ->
                lists:reverse([{stop, 0} | Acc]);
            run(Chars, Acc) ->
                case step(Chars, Acc) of
                    {rescan, Rest} -> run(Rest, Acc);
                    {Token, Rest} -> run(Rest, [Token | Acc])
                end.
            step([Head|Rest], _Acc) -> {Head, Rest}.
            trap(Pid) when is_pid(Pid) -> erlang:link(Pid);
            trap(Other) -> {step(Other, []), Other}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_entry_tail_list.erl");
        object code = Code(beam);
        byte[] opcodes = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int branch = Array.IndexOf(opcodes, BeamEmissionFormat.IsNil);
        int firstFrame = Array.IndexOf(opcodes, BeamEmissionFormat.Allocate);
        int tail = Array.IndexOf(opcodes, BeamEmissionFormat.TailExternalCall);
        int trappingTail = Array.IndexOf(opcodes, BeamEmissionFormat.TailExternalCallWithFrame);
        byte[] prefix = [BeamEmissionFormat.IsNil, BeamEmissionFormat.TestHeap, BeamEmissionFormat.PutList];
        if (branch < 0 || !opcodes.AsSpan(branch).StartsWith(prefix)
            || tail <= branch || firstFrame <= tail || trappingTail <= firstFrame)
        {
            throw new InvalidOperationException(BeamEntryExternalTailDiagnostics.BranchFrame);
        }
    }

    private static void CheckTrappingTailAllocation()
    {
        const string source = """
            -module(beam_trapping_fallback).
            -export([pick/3]).
            pick(true, Pid, Tail) -> pair(Pid, Tail);
            pick(false, Pid, Tail) -> fake_dep:observe(Pid, Tail);
            pick(Head, Pid, Tail) -> erlang:exit(Pid, [Head | Tail]).
            pair(Pid, Tail) -> {Pid, Tail}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_trapping_fallback.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> combinedFrame =
        [
            BeamEmissionFormat.AllocateHeap, 0x00, 0x20, 0x30,
            BeamEmissionFormat.PutList
        ];
        if (code.AsSpan().IndexOf(combinedFrame) < 0)
        {
            throw new InvalidOperationException(BeamEntryExternalTailDiagnostics.TrappingHeapFrame);
        }
    }
}
