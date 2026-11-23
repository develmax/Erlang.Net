using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectionTemporaryDeathChecks
{
    internal const string TestName = "compiler/beam/forward-projection-through-temporary-overwrite";
    private const string KeptCopy = "The projection was copied even though the next assignment replaced its temporary.";
    private const string LostValue = "The compiler discarded a projection still read by an outgoing argument.";
    private const string Source = """
        -module(beam_projection_temporary_death).
        -export([reset/1, keep/1]).
        -record(reading,{device,value,timer,maximum}).
        reset(#reading{device=Device}) ->
            driver:set(Device,0).
        keep(#reading{device=Device}) ->
            driver:set(Device,Device).
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_projection_temporary_death.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        object projection = Body("reset").Single(instruction => Opcode(instruction) == GetTupleElement);
        if (Operands(projection)[2] is not ("x", 0))
        {
            throw new InvalidOperationException(KeptCopy);
        }

        object[] kept = Body("keep");
        if (!kept.Any(instruction => Opcode(instruction) == Move && Operands(instruction) is [("x", 1), ("x", 0)]))
        {
            throw new InvalidOperationException(LostValue);
        }

        object[] Body(string name)
        {
            object entry = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(entry, "FirstInstructionIndex");
            int end = (int)Property(entry, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static (string Kind, int Value)[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
            .Select(operand => ((string)Property(operand, "Kind"), (int)(BigInteger)Property(operand, "Value"))).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
