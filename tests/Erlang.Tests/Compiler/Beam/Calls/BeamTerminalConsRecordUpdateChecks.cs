using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalConsRecordUpdateChecks
{
    internal const string TestName = "compiler/beam/forwards-a-terminal-cons-to-its-record-update";
    private const string WrongLayout = "The record update must use the constructed cons and keep independent input roots.";

    internal static Task Run()
    {
        Check(tailProjection: false, usesX1: false);
        Check(tailProjection: true, usesX1: false);
        Check(tailProjection: true, usesX1: true);
        if (Rewrite(Fixture(readsSavedLater: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        static void Check(bool tailProjection, bool usesX1)
        {
            byte[] expected = Fixture(rewritten: true, tailProjection: tailProjection, usesX1: usesX1);
            if (Rewrite(Fixture(tailProjection: tailProjection, usesX1: usesX1)) is not { } actual
                || !actual.AsSpan().SequenceEqual(expected) || Rewrite(actual) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }
    }

    private static byte[] Fixture(
        bool rewritten = false,
        bool tailProjection = true,
        bool usesX1 = false,
        bool readsSavedLater = false
    )
    {
        byte scratch = X(usesX1 ? 2 : 1);
        byte[] headAndTail = !tailProjection ? [PutList, X(0), Y(1), X(0)]
            : rewritten
                ? [
                    PutTuple, X(0), ExtendedListOperand, U(1), A(1),
                    GetTupleElement, Y(1), U(2), scratch, PutList, X(0), scratch, X(0)
                ]
                : [
                    PutTuple, X(0), ExtendedListOperand, U(1), A(1), Move, X(0), Y(0),
                    GetTupleElement, Y(1), U(2), X(0), PutList, Y(0), X(0), X(0)
                ];
        byte[] store = rewritten ? [] : [Move, X(0), Y(0)];
        byte[] updatedField = rewritten ? [ExtendedTypedRegisterOperand, X(0), U(0)] : [Y(0)];
        byte[] otherField = usesX1 ? [U(2), X(1)] : [];
        byte[] observe = readsSavedLater ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2), Move, X(0), Y(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            TestHeap, U(8), U(2), .. headAndTail, .. store,
            UpdateRecord, A(rewritten ? 3 : 2), U(3), Y(1), X(0), ExtendedListOperand, U(usesX1 ? 4 : 2),
            .. otherField, U(3), .. updatedField, .. observe,
            Deallocate, U(2), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            3,
            1,
            code
        );
        var stack = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordStack = (start, end, slot) => stack.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            recordStack
        );
        Func<int> copyHint = () => 3;
        Func<int> consType = () => 0;

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteTerminalConsRecordUpdates",
            code,
            body,
            stack,
            2,
            copyHint,
            consType
        );
    }
}
