using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomOwnedNestedRecordChecks
{
    internal const string TestName = "compiler/beam/branch-owned-nested-records-after-atom-selection";
    private const string EarlyFrame = "A checked nested record allocated before its atom branch was selected.";
    private const string SharedOverlapping = "Overlapping leading atoms were treated as disjoint.";
    private const string Source = """
        -module(beam_atom_owned_nested_records).
        -export([update/2, shared/2]).
        -record(state,{first,second}).
        -record(part,{value}).
        update(tick, S=#state{first=P=#part{value=Value}}) ->
            service:emit(Value),
            {ok,S#state{first=P#part{value=Value+1}}};
        update(stop, S=#state{second=P=#part{value=Value}}) ->
            service:close(Value),
            service:notice(Value),
            {ok,S#state{second=P#part{value=0}}}.
        shared(change, S=#state{first=P=#part{value=Value}}) when is_integer(Value) ->
            service:emit(Value),
            {ok,S#state{first=P#part{value=Value+1}}};
        shared(change, S=#state{second=P=#part{value=Value}}) ->
            service:close(Value),
            {ok,S#state{second=P#part{value=0}}}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_atom_owned_nested_records.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        byte[] body = Body("update");
        int choice = Array.IndexOf(body, SelectValue);
        if (choice < 0 || choice >= Array.IndexOf(body, IsTaggedTuple)
            || body.Count(opcode => opcode == Allocate) != 2)
        {
            throw new InvalidOperationException(EarlyFrame);
        }

        if (Body("shared").TakeWhile(opcode => opcode is not (Allocate or AllocateHeap)).Contains(SelectValue))
        {
            throw new InvalidOperationException(SharedOverlapping);
        }

        byte[] Body(string name)
        {
            object selected = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(selected, "FirstInstructionIndex");
            int end = (int)Property(selected, "EndInstructionIndex");

            return instructions[start..end]
                .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        }
    }
}
