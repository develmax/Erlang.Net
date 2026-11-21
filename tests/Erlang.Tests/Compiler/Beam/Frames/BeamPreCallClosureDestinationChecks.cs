using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPreCallClosureDestinationChecks
{
    private const string Pass = "RedirectPreCallClosureDestinations";
    private const string Expected = "Expected the closure in its outgoing X with the existing heap reservation and retained Y root.";
    private const string Unsafe = "The closure must keep its owned Y when the outgoing register or closure is still live.";

    internal static void Run(BeamModuleCompiler compiler)
    {
        byte[] independent = [Move, X(1), X(2)];
        if (Rewrite(compiler, Fixture(beforeReload: independent)) is not { } rewritten
            || !rewritten.SequenceEqual(Fixture(compact: true, beforeReload: independent)))
        {
            throw new InvalidOperationException(Expected);
        }

        CheckUnchanged(compiler, Fixture(beforeReload: [Move, X(0), X(2)]));
        CheckUnchanged(compiler, Fixture(beforeReload: [TestHeap, U(1), U(1)]));
        CheckUnchanged(compiler, Fixture(beforeReload: [InitYRegisters, ExtendedListOperand, U(1), Y(0)]));
        CheckUnchanged(compiler, Fixture(afterReload: [Move, X(1), X(0)]));
        CheckUnchanged(compiler, Fixture(afterCall: [Move, Y(0), X(0)]));
        CheckUnchanged(compiler, Fixture(captureOutgoing: true));
        CheckUnchanged(compiler, Fixture(interiorEntry: true));
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            Pass,
            code,
            5
        );

    private static void CheckUnchanged(BeamModuleCompiler compiler, byte[] code)
    {
        if (Rewrite(compiler, code) is not null)
        {
            throw new InvalidOperationException(Unsafe);
        }
    }

    private static byte[] Fixture(
        bool compact = false,
        byte[]? beforeReload = null,
        byte[]? afterReload = null,
        byte[]? afterCall = null,
        bool captureOutgoing = false,
        bool interiorEntry = false
    )
    {
        int width = compact ? 1 : 2;
        byte retained = Y(compact ? 0 : 1);
        byte destination = compact ? X(0) : Y(0);
        byte[] reload = compact ? [] : [Move, Y(0), X(0)];
        byte[] sharedLabel = interiorEntry ? [Label, U(3)] : [];
        byte[] alternateEntry = interiorEntry
            ? [InitYRegisters, ExtendedListOperand, U(1), Y(0), IsAtom, F(3), X(0)]
            : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            AllocateHeap, U(width), (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
            U(AllocationWords), U(0), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(2),
            Move, X(1), retained,
            .. alternateEntry,
            MakeFun, U(0), destination, ExtendedListOperand, U(1), captureOutgoing ? X(0) : retained,
            .. (beforeReload ?? []),
            .. sharedLabel,
            .. reload,
            .. (afterReload ?? []),
            Line, U(1),
            ExternalCall, U(1), U(0),
            .. (afterCall ?? []),
            Move, retained, X(0),
            Deallocate, U(width), Return,
            EndCode
        ];
    }
}
