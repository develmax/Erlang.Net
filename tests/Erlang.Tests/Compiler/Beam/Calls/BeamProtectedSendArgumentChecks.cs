using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProtectedSendArgumentChecks
{
    internal const string TestName = "compiler/beam/builds-protected-message-tuple-in-outgoing-x1";
    private const string Module = "beam_protected_send_argument_fixture";
    private const string WrongLayout = "Protected message construction must preserve the receiver and all live stack operands.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        if (Rewrite(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(direct: true))
            || Rewrite(compiler, actual) is not null || Rewrite(compiler, Fixture(readMessageLater: true)) is not null
            || Rewrite(compiler, Fixture(protectedSend: false)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (RewriteRetained(compiler, RetainedFixture()) is not { } retained
            || !retained.SequenceEqual(RetainedFixture(direct: true))
            || RewriteRetained(compiler, retained) is not null
            || RewriteRetained(compiler, RetainedFixture(readStackLater: true)) is not null
            || RewriteRetained(compiler, RetainedFixture(sameReceiver: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (RewriteOwned(compiler, OwnedFixture()) is not { } owned
            || !owned.SequenceEqual(OwnedFixture(direct: true))
            || RewriteOwned(compiler, owned) is not null
            || RewriteOwned(compiler, OwnedFixture(reread: true)) is not null
            || RewriteOwned(compiler, OwnedFixture(sameReceiver: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool direct = false, bool readMessageLater = false, bool protectedSend = true)
    {
        byte[] install = protectedSend ? [TryEnter, Y(2), F(3)] : [];
        byte[] tryEnd = protectedSend ? [TryEnd, Y(2)] : [];
        byte[] save = direct ? [] : [Move, X(0), Y(1)];
        byte[] reload = direct ? [] : [Move, Y(1), X(1)];
        byte[] after = readMessageLater ? [Move, Y(1), X(0)] : [];
        byte[] landing = protectedSend
            ? [Label, U(3), TryCase, Y(2), .. after, Deallocate, U(3), Return]
            : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(3), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(1), Move, X(0), Y(0),
            .. install, TestHeap, U(3), U(0), PutTuple, direct ? X(1) : X(0),
            ExtendedListOperand, U(2), A(1), Y(0), .. save,
            Move, Y(0), X(0), .. reload, Line, U(1), Send, .. tryEnd,
            Deallocate, U(3), Return, .. landing, EndCode
        ];
    }

    private static byte[] RetainedFixture(
        bool direct = false,
        bool readStackLater = false,
        bool sameReceiver = false
    )
    {
        byte[] save = direct ? [] : [Move, X(0), Y(1)];
        byte[] reload = direct ? [TrimFrame, U(3), U(0)] : [Move, Y(1), X(1)];
        byte[] after = readStackLater ? [Move, Y(2), X(0)] : [Move, A(1), X(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            Allocate, U(3), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(1),
            Move, X(0), Y(0), Move, X(1), Y(2), ExternalCall, U(0), U(0),
            .. save, TestHeap, U(4), U(direct ? 1 : 0), PutTuple, direct ? X(1) : X(0),
            ExtendedListOperand, U(3), A(1), Y(2), direct ? X(0) : Y(1), .. save,
            Move, sameReceiver ? Y(1) : Y(0), X(0), .. reload, Line, U(1), Send, .. after,
            Deallocate, U(direct ? 0 : 3), Return, EndCode
        ];
    }

    private static byte[]? RewriteRetained(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "BuildRetainedResultSendTuple",
            code,
            4
        );

    private static byte[] OwnedFixture(bool direct = false, bool reread = false, bool sameReceiver = false)
    {
        byte[] initialize = direct ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] spill = direct ? [] : [Move, X(0), Y(1)];
        byte[] reload = direct ? [] : [Move, Y(1), X(1)];
        byte[] after = reread ? [Move, Y(1), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(direct ? 1 : 2), U(2), Move, X(0), Y(0), .. initialize,
            TestHeap, U(3), U(2), PutTuple, direct ? X(1) : X(0), ExtendedListOperand, U(2), A(1), X(1),
            .. spill, Move, sameReceiver ? Y(1) : Y(0), X(0), .. reload, Line, U(1), Send, .. after,
            ExternalCall, U(0), U(0), Deallocate, U(direct ? 1 : 2), Return, EndCode
        ];
    }

    private static byte[]? RewriteOwned(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "RemoveOwnedSendMessageSlot",
            code,
            4
        );

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "BuildTupleInProtectedSendArgument",
            code,
            4
        );
}
