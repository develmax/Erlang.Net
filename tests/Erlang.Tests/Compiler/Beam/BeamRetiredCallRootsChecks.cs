using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetiredCallRootsChecks
{
    internal const string TestName = "compiler/beam/orders-existing-retired-y-roots-at-ordinary-calls";
    private const string WrongOrder = "Retiring stack roots must be cleared after their last outgoing read.";

    internal static Task Run()
    {
        byte[] entry = Start(
            1,
            2,
            2,
            2
        );
        byte[] frame = [Allocate, U(2), U(2), Move, X(0), Y(0), Move, X(1), Y(1)];
        byte[] outgoing = [Move, Y(0), X(1), Move, A(2), X(2)];
        byte[] first = [Move, A(2), X(0)];
        byte[] call = [Line, U(1), ExternalCall, U(3), U(0)];
        byte[] clear = [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] ending =
        [
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), Y(1), X(0),
            Deallocate, U(2), Return, EndCode
        ];
        byte[] before = [.. entry, .. frame, .. outgoing, .. first, .. call, .. clear, .. ending];
        byte[] expected = [.. entry, .. frame, .. outgoing, .. clear, .. first, .. call, .. ending];
        byte[] reread = [Move, Y(0), X(0)];
        byte[] lastRead = [.. entry, .. frame, .. outgoing, .. reread, .. call, .. clear, .. ending];
        byte[] beforeLine = [.. entry, .. frame, .. outgoing, .. reread, .. clear, .. call, .. ending];
        byte[] unrelated = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] protectedEntry = [CatchEnter, Y(1), F(2), CatchEnd, Y(1)];
        if (Rewrite(before) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(lastRead) is not { } lateClear || !lateClear.AsSpan().SequenceEqual(beforeLine)
            || Rewrite(expected) is not null
            || Rewrite([.. entry, .. frame, .. outgoing, .. first, .. call, .. unrelated, .. ending]) is not null
            || Rewrite([.. entry, .. frame, .. protectedEntry, .. outgoing, .. first, .. call, .. clear, .. ending]) is not null)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        var compiler = new BeamModuleCompiler("retired_call_root_probe");
        _ = Call(compiler, "Intern", "run");

        return (byte[]?)Call(
            compiler,
            "ScheduleRetiredCallArgumentRoots",
            new ReadOnlyMemory<byte>(bytes),
            3
        );
    }
}
