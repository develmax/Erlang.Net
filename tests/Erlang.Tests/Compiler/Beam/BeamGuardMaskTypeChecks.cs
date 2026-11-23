using System.Collections;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamGuardMaskTypeChecks
{
    internal const string TestName = "compiler/beam/guard-negative-mask-keeps-integer-proof";
    private const string Module = "beam_guard_band";
    private const string Failure = "The guard comparison lost the integer proof from its successful negative mask.";

    internal static Task Run()
    {
        string file = Module + ".erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        byte[] image = CodeGeneration.CompileBeamModule(source, file);
        byte[] reference = BeamModuleEmissionChecks.Read(Module + ".beam");
        byte[] expected = Chunks(reference).Single(chunk => chunk.Id == "Type").Data;
        byte[] actual = Chunks(image).Single(chunk => chunk.Id == "Type").Data;
        var instructions = ((IEnumerable)Property(Code(image), "Instructions")).Cast<object>().ToArray();
        int mask = Array.FindIndex(instructions, instruction => Opcode(instruction) == BeamEmissionFormat.GcBifTwo);
        if (!actual.AsSpan().SequenceEqual(expected) || mask < 0 || mask + 1 == instructions.Length
            || Opcode(instructions[mask + 1]) != BeamEmissionFormat.ExactEqualTest
            || Property(((IEnumerable)Property(instructions[mask + 1], "Operands")).Cast<object>().ElementAt(1), "TypeIndex") is not 1)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
