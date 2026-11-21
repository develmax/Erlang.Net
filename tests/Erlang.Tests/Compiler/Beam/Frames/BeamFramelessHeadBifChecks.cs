using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFramelessHeadBifChecks
{
    internal const string TestName = "compiler/beam/keeps-unconditional-leaf-head-bif-frameless";
    private const string Fixture = "beam_frameless_head_bif_probe.erl";
    private const string WrongFrame = "A leaf head BIF must retain its native failure and tail call without a stack frame.";
    private const string WrongElement = "A fixed tuple projection must retain its native failure and frameless tail.";
    private const string WrongControl = "Other BIFs, returning calls and protected paths must retain their own frames.";

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        object[] run = Read("run");
        int head = Array.FindIndex(run, instruction => Opcode(instruction) == BifOne);
        if (HasFrame(run) || head < 1 || Opcode(run[head - 1]) != Line
            || !run.Any(instruction => Opcode(instruction) == TailLocalCall)
            || !run.Any(instruction => Opcode(instruction) == TestHeap))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] element = Read("by_element");
        int projection = Array.FindIndex(element, instruction => Opcode(instruction) == BifTwo);
        if (HasFrame(element) || projection < 1 || Opcode(element[projection - 1]) != Line
            || !element.Any(instruction => Opcode(instruction) == TailLocalCall))
        {
            throw new InvalidOperationException(WrongElement);
        }

        if (!HasFrame(Read("dynamic_element")) || !HasFrame(Read("preceded")) || !HasFrame(Read("protected")))
        {
            throw new InvalidOperationException(WrongControl);
        }

        return Task.CompletedTask;

        object[] Read(string name)
        {
            int index = Array.FindIndex(functions, function => (string)Property(function, "Name") == name);
            int first = (int)Property(functions[index], "FirstInstructionIndex");
            int next = index + 1 < functions.Length
                ? (int)Property(functions[index + 1], "FirstInstructionIndex") : instructions.Length;

            return instructions[first..next];
        }
    }

    private static bool HasFrame(IEnumerable<object> instructions) =>
        instructions.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    internal const string Source = """
        -module(beam_frameless_head_bif_probe).
        -export([run/3, by_element/3, dynamic_element/4, preceded/3, protected/1, make/3, produce/1]).

        run(Input, Stack, Tag) ->
            [_,_ | Tail] = Input,
            [_, First | Rest] = Stack,
            make(hd(Tail), [First | Rest], Tag).

        by_element(Input, Stack, Tag) ->
            [_,_ | Tail] = Input,
            [_, First | Rest] = Stack,
            make(element(1, Tail), [First | Rest], Tag).

        dynamic_element(Input, Stack, Tag, Index) ->
            [_,_ | Tail] = Input,
            [_, First | Rest] = Stack,
            make(element(Index, Tail), [First | Rest], Tag).

        preceded(Input, Stack, Tag) ->
            [_,_ | Tail] = Input,
            [_, First | Rest] = Stack,
            Produced = ?MODULE:produce(Tail),
            make(hd(Produced), [First | Rest], Tag).

        protected(Value) ->
            try hd(Value) of Result -> {ok,Result}
            catch error:badarg -> rejected
            end.

        make(Value, List, Tag) -> {Tag,Value,List}.
        produce(Value) -> Value.
        """;
}
