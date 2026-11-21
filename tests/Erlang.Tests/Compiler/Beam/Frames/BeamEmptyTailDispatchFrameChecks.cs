using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEmptyTailDispatchFrameChecks
{
    internal const string TestName = "compiler/beam/removes-empty-frames-across-proven-tail-dispatch";
    private const string WrongFrame = "The guarded tail dispatch retained an empty frame or removed a required call frame.";
    private const string WrongValue = "The guarded recursion changed list membership.";
    private const string Source = """
        -module(beam_empty_tail_dispatch_frame).
        -export([member/1, trap/1]).
        -record(state, {options}).
        member(S) when is_record(S, state) ->
            member(S#state.options);
        member(Options) ->
            lists:member(chosen, Options).
        trap(S) when is_record(S, state) ->
            trap(S#state.options);
        trap(Pid) ->
            erlang:link(Pid).
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_empty_tail_dispatch_frame.erl");
        object code = Code(bytes);
        var entries = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .OrderBy(entry => (int)Property(entry, "FirstInstructionIndex"))
            .ToArray();
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] member = Opcodes("member");
        byte[] trapping = Opcodes("trap");
        if (!member.Contains(TailLocalCall) || !member.Contains(TailExternalCall)
            || member.Any(opcode => opcode is Allocate or AllocateHeap or Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame)
            || !trapping.Contains(TailExternalCallWithFrame) || !trapping.Any(opcode => opcode is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        var module = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term found = Term.List(Term.A("chosen"));
            Term missing = Term.List(Term.A("other"));
            if (!Term.A("true").Equals(await module.Invoke(context, "member", found))
                || !Term.A("true").Equals(await module.Invoke(context, "member", Term.Tuple(Term.A("state"), found)))
                || !Term.A("false").Equals(await module.Invoke(context, "member", Term.Tuple(Term.A("state"), missing))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });

        byte[] Opcodes(string name)
        {
            int entry = Array.FindIndex(entries, function => (string)Property(function, "Name") == name);
            int first = (int)Property(entries[entry], "FirstInstructionIndex");
            int end = entry + 1 < entries.Length
                ? (int)Property(entries[entry + 1], "FirstInstructionIndex") : instructions.Length;

            return instructions[first..end].Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        }
    }
}
