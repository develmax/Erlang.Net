using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleMapEntryFrameChecks
{
    internal const string TestName = "compiler/beam/checks-tuple-and-map-before-entering-owning-frame";
    private const string IncorrectEntryFrame = "Tuple and map checks must finish before their branch allocates its frame.";

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(
            BeamEntryTupleLivenessChecks.Source,
            BeamEntryTupleLivenessChecks.Fixture
        ));
        object[] operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = operations.Select(operation => (byte)Property(Property(operation, "Definition"), "Opcode")).ToArray();
        int firstDispatch = Array.IndexOf(opcodes, SelectTupleArity);
        if (firstDispatch < 0)
        {
            throw new InvalidOperationException(IncorrectEntryFrame);
        }

        int map = Array.IndexOf(opcodes, GetMapElements, firstDispatch + 1);
        int frame = Array.IndexOf(opcodes, Allocate, firstDispatch + 1);
        if (map < 0 || frame < map)
        {
            throw new InvalidOperationException(IncorrectEntryFrame);
        }

        object[] operands = ((IEnumerable)Property(operations[frame], "Operands")).Cast<object>().ToArray();
        if ((BigInteger)Property(operands[0], "Value") != 2)
        {
            throw new InvalidOperationException(IncorrectEntryFrame);
        }

        return Task.CompletedTask;
    }
}
