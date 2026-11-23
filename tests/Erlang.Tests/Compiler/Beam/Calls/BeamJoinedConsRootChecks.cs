using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamJoinedConsRootChecks
{
    internal const string TestName = "compiler/beam/retains-a-joined-value-as-the-cons-heap-root";
    private const string WrongLayout = "The joined value must remain rooted in its original register when the saved slot is dead.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_joined_cons_root");
        _ = Call(compiler, "Intern", "first");
        if (Apply(Fixture()) is not { } rewritten || !rewritten.SequenceEqual(Fixture(useRegister: true))
            || Apply(rewritten) is not null || Apply(Fixture(laterRead: true)) is not null
            || Apply(Fixture(uninitializedSlot: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "KeepJoinedResultThroughCons",
            code,
            5
        );
    }

    private static byte[] Fixture(bool useRegister = false, bool laterRead = false, bool uninitializedSlot = false)
    {
        byte[] spill = useRegister ? [] : [Move, X(0), Y(0)];
        byte[] restore = laterRead ? [Move, Y(0), X(0)] : [];
        byte[] initial = uninitializedSlot ? [U(1), Y(1)] : [U(2), Y(0), Y(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, .. initial,
            ExactEqualTest, F(3), X(0), A(2), Move, A(2), X(0), Jump, F(4),
            Label, U(3), Move, X(1), X(0), Label, U(4), .. spill,
            TestHeap, U(4), U(useRegister ? 1 : 0), PutList, useRegister ? X(0) : Y(0), A(0), X(0),
            PutList, Y(1), X(0), X(0), ExternalCall, U(1), U(0), .. restore,
            Deallocate, U(2), Return, EndCode
        ];
    }
}
