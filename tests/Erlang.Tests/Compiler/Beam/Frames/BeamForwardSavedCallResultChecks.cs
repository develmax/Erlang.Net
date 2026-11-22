using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamForwardSavedCallResultChecks
{
    internal const string TestName = "compiler/beam/forwards-guarded-call-result-before-an-independent-collector";
    private const string Unexpected = "The guarded result must stay in X until its final outgoing copy.";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not byte[] forwarded || !forwarded.SequenceEqual(Fixture(forwarded: true))
            || Rewrite(forwarded) is not null || Rewrite(Fixture(collectorUsesSlot: true)) is not null
            || Rewrite(Fixture(collectorBranches: true)) is not null
            || Rewrite(Fixture(joined: true)) is not byte[] joined
            || !joined.SequenceEqual(Fixture(forwarded: true, joined: true))
            || Rewrite(Fixture(joined: true, nonCallPredecessor: true)) is not null)
        {
            throw new InvalidOperationException(Unexpected);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            7,
            1,
            code
        );
        var stack = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> record = (start, end, slot) => stack.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            record
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ForwardSavedCallResults",
            code,
            body,
            stack
        );
    }

    private static byte[] Fixture(
        bool forwarded = false,
        bool collectorUsesSlot = false,
        bool collectorBranches = false,
        bool joined = false,
        bool nonCallPredecessor = false
    )
    {
        byte[] save = forwarded ? [] : [Move, X(0), Y(0)];
        byte result = forwarded ? X(0) : Y(0);
        byte[] alternate = nonCallPredecessor ? [Move, I(4), X(0)] : [ExternalCall, U(1), U(0)];
        byte[] produce = joined
            ? [ExactEqualTest, F(4), X(0), I(0), ExternalCall, U(1), U(0), Jump, F(5), Label, U(4), .. alternate, Label, U(5)]
            : [ExternalCall, U(1), U(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(1), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            .. produce, .. save, IsGreaterOrEqual, F(3), result, I(0),
            Move, result, X(1), Move, A(1), X(0), ExternalCall, U(2), U(1),
            GcBifTwo, F(collectorBranches ? 3 : 0), U(1), U(2), collectorUsesSlot ? Y(0) : X(0), I(1), X(0),
            Deallocate, U(1), Return,
            Label, U(3), Move, I(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }
}
