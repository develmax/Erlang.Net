using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNeutralTailFrameChecks
{
    internal const string TestName = "compiler/beam/keeps-heap-neutral-tail-arguments-frameless";
    private const string Fixture = "neutral_tail_probe.erl";
    private const string IncorrectTailFrames = "Only the trapping or continuing call requires a frame.";
    private const string Source = """
        -module(neutral_tail_probe).
        -export([render/1, trap/0, later/1]).

        render(pid) -> io_lib:format("~p",[self()]);
        render(node) -> io_lib:format("~p",[node()]);
        render(Value) -> io_lib:format("~p",[Value]).

        trap() -> link(self()).

        later(Value) ->
            io_lib:format("~p",[Value]),
            io_lib:format("~p",[node()]).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        byte[] render = ReadOpcodes("render");
        byte[] trap = ReadOpcodes("trap");
        byte[] later = ReadOpcodes("later");
        if (render.Count(opcode => opcode == TailExternalCall) != 3
            || render.Any(opcode => opcode is Allocate or AllocateHeap or TailExternalCallWithFrame)
            || !trap.Contains(Allocate) || !trap.Contains(TailExternalCallWithFrame)
            || !later.Contains(AllocateHeap) || !later.Contains(ExternalCall)
            || !later.Contains(TailExternalCallWithFrame))
        {
            throw new InvalidOperationException(IncorrectTailFrames);
        }

        return Task.CompletedTask;

        byte[] ReadOpcodes(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
