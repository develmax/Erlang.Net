using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSavedCallResultSwapChecks
{
    internal const string TestName = "compiler/beam/swaps-a-retained-call-result-with-a-saved-receiver";
    private const string Fixture = "beam_saved_result_swap.erl";
    private const string UnexpectedSchedule = "The retained result and receiver were not placed in the pinned argument order.";
    private const string UnnecessarySwap = "An argument already in its destination register was swapped.";

    private const string Source = """
        -module(beam_saved_result_swap).
        -export([run/1]).
        -record(state,{parent,config,grid}).

        run(Config) ->
            Parent = proplists:get_value(parent,Config),
            Panel = peer:panel(Parent,[]),
            Main = peer:sizer(vertical),
            Sizer = peer:static_sizer(vertical,Panel,[{label,"Grid"}]),
            Grid = grid(Panel),
            Options = [{flag,8192},{proportion,1}],
            peer:add(Sizer,Grid,Options),
            peer:add(Main,Sizer,Options),
            peer:show(Panel,Main),
            {Panel,#state{parent=Panel,config=Config,grid=Grid}}.

        grid(Panel) -> peer:grid(Panel).
        """;

    internal static Task Run()
    {
        var instructions = Read(Source);
        int swap = Array.FindIndex(instructions, instruction => Opcode(instruction) == Swap);
        if (swap < 3 || Opcode(instructions[swap - 1]) != Move || Opcode(instructions[swap - 2]) != Move
            || Opcode(instructions[swap - 3]) != Move || !HasOperand(
                instructions[swap],
                0,
                "x",
                0
            )
            || !HasOperand(
                instructions[swap],
                1,
                "x",
                1
            ))
        {
            throw new InvalidOperationException(UnexpectedSchedule);
        }

        string control = Source.Replace("peer:add(Sizer,Grid,Options)", "peer:add(Grid,Sizer,Options)", StringComparison.Ordinal);
        if (Read(control).Any(instruction => Opcode(instruction) == Swap))
        {
            throw new InvalidOperationException(UnnecessarySwap);
        }

        return Task.CompletedTask;
    }

    private static object[] Read(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static bool HasOperand(
        object instruction,
        int index,
        string kind,
        int value
    )
    {
        var operands = ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

        return (string)Property(operands[index], "Kind") == kind
            && (int)(BigInteger)Property(operands[index], "Value") == value;
    }
}
