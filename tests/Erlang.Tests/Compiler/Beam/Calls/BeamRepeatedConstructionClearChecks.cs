using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRepeatedConstructionClearChecks
{
    private const int SyntheticAtomCount = 2;
    internal const string TestName = "compiler/beam/removes-a-construction-clear-only-for-initialized-slots";

    internal static Task Run()
    {
        byte[] expected = Fixture(remove: true);
        if (Rewrite(Fixture()) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(needed: true)) is not null
            || Rewrite(Fixture(stackWrite: true)) is not null)
        {
            throw new InvalidOperationException("A repeated construction clear must only remove already initialized slots.");
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool remove = false, bool needed = false, bool stackWrite = false)
    {
        byte[] first = needed ? [Y(0), Y(1)] : [Y(0), Y(1), Y(2)];
        byte[] repeated = remove ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(2)];
        byte[] tuple = stackWrite ? [Move, X(0), Y(2)] : [PutTuple, X(0), ExtendedListOperand, U(2), A(1), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            AllocateHeap, U(3), U(5), U(2), InitYRegisters, ExtendedListOperand, U(first.Length), .. first,
            .. tuple, .. repeated, PutList, X(0), X(1), X(0), Deallocate, U(3), Return,
            EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            SyntheticAtomCount,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteRepeatedConstructionClears",
            code,
            body
        );
    }
}
