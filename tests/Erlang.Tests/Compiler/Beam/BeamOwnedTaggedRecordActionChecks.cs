using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedTaggedRecordActionChecks
{
    internal const string TestName = "compiler/beam/owns-checked-tagged-record-action-frames";
    private const string ShapeError = "The tagged record action allocated before checking its repeated field.";
    private const string Source = """
        -module(beam_owned_tagged_record_actions).
        -export([event/2]).
        -record(state, {id, manager, endpoint}).

        event({opened,Id,Manager}, State) ->
            {ok,State#state{id=Id,manager=Manager}};
        event({packet,Endpoint,Bytes}, #state{id=Id,manager=Manager,endpoint=Endpoint}=State) ->
            service:send(Manager, Id, Bytes),
            service:setopts(Endpoint, [{active,once}]),
            {ok,State};
        event({closed,Endpoint}, #state{id=Id,manager=Manager,endpoint=Endpoint}=State) ->
            service:send_eof(Manager, Id),
            {stop,Id,State#state{endpoint=undefined}}.
        """;

    internal static void Run()
    {
        string independent = Source.Replace(",endpoint=Endpoint", "", StringComparison.Ordinal);
        if (independent == Source || !HasOwnedFrames(Source) || HasOwnedFrames(independent))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasOwnedFrames(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_owned_tagged_record_actions.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var allocations = instructions.Select((instruction, index) => (instruction, index))
            .Where(item => Opcode(item.instruction) == Allocate).ToArray();

        return allocations.Length == 2
            && IsCheckedFrame(allocations[0].index, 5)
            && IsCheckedFrame(allocations[1].index, 2)
            && instructions.Count(instruction => Opcode(instruction) == TrimFrame) == 1
            && instructions.Count(instruction => Opcode(instruction) == BadRecord) == 1;

        bool IsCheckedFrame(int index, int live) => index > 0
            && Opcode(instructions[index - 1]) == ExactEqualTest
            && ((IEnumerable)Property(instructions[index], "Operands")).Cast<object>()
                .Select(operand => (int)(BigInteger)Property(operand, "Value")).SequenceEqual([2, live]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
