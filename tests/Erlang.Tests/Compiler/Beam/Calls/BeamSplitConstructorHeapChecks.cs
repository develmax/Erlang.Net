using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSplitConstructorHeapChecks
{
    internal const string TestName = "compiler/beam/coalesces-heap-reservations-across-pure-constructor-spill";
    private const string WrongLayout = "The fused heap reservation lost an input root or an observable cons result.";

    internal static Task Run()
    {
        AssertLayout(observeCons: false);
        AssertLayout(observeCons: true);
        if (Rewrite(Fixture(interrupt: true)) is not null || Rewrite(Fixture(secondLive: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        static void AssertLayout(bool observeCons)
        {
            byte[] original = Fixture(observeCons: observeCons);
            byte[] expected = Fixture(fused: true, observeCons: observeCons);
            if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected) || Rewrite(actual) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }
    }

    private static byte[] Fixture(
        bool fused = false,
        bool observeCons = false,
        bool interrupt = false,
        bool secondLive = false
    )
    {
        byte[] prefix = [TestHeap, U(fused ? 5 : 3), U(1)];
        byte[] pair = [PutTuple, X(0), ExtendedListOperand, U(2), A(1), X(0)];
        byte[] separator = interrupt ? [Line, U(1)] : [];
        byte[] additional = fused ? [] : [TestHeap, U(2), U(secondLive ? 1 : 0)];
        byte[] construction = fused && !observeCons
            ? [GetTupleElement, Y(1), U(1), X(1), .. pair, PutList, X(0), X(1), Y(0)]
            : [
                .. pair, Move, X(0), Y(0), .. separator, .. additional,
                GetTupleElement, Y(1), U(1), X(0), PutList, Y(0), X(0), X(0), Move, X(0), Y(0)
            ];
        byte[] observe = observeCons ? [Move, X(0), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(0), Y(1),
            .. prefix, .. construction, .. observe, Move, Y(0), X(0),
            Deallocate, U(2), Return, EndCode
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
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteSplitConstructorHeaps",
            code,
            body
        );
    }
}
