using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryAndCallResultSlotChecks
{
    internal const string TestName = "compiler/beam/preserves-entry-slot-order-before-an-in-frame-call-result";
    internal const string Fixture = "beam_entry_and_call_result_slots.erl";
    private const string ChangedSlots = "The incoming argument and later call result used the wrong frame slots.";

    internal const string Source = """
        -module(beam_entry_and_call_result_slots).
        -export([select/2]).

        select(Base, Name) ->
            Path = filename:join(Base, Name),
            case filelib:is_dir(Path) of
                true -> Path;
                false -> Base
            end.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static Task Run()
    {
        object code = Code(CompileFixture());
        object[] body = Items(code, "Instructions").ToArray();
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        if (frame < 0 || Opcode(body[frame + 1]) != InitYRegisters || Opcode(body[frame + 2]) != Move
            || Value(Items(body[frame + 1], "Operands").Last()) != 0
            || Value(Items(body[frame + 2], "Operands").Last()) != 1)
        {
            throw new InvalidOperationException(ChangedSlots);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
