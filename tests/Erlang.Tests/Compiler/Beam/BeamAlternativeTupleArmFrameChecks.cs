using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAlternativeTupleArmFrameChecks
{
    internal const string TestName = "compiler/beam/checks-tag-before-allocating-alternative-tuple-arm";
    private const string WrongLayout = "The alternative tuple arm must reach function_clause with the original unframed arguments.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_alternative_tuple_arm_frame");
        _ = Call(compiler, "Intern", "first");
        _ = Call(compiler, "Intern", "tag");
        if (Apply(Fixture()) is not ValueTuple<ReadOnlyMemory<byte>, int> ordered
            || ordered.Item2 != 4 || !ordered.Item1.Span.SequenceEqual(Fixture(ordered: true))
            || Apply(Fixture(ordered: true), 4) is not null
            || Apply(Fixture(wrongRestore: true)) is not null || Apply(Fixture(sharedCleanup: true)) is not null
            || Apply(Fixture(earlyHeap: true)) is not ValueTuple<ReadOnlyMemory<byte>, int> initialized
            || !initialized.Item1.Span.SequenceEqual(Fixture(ordered: true, earlyHeap: true)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code, int labels = 5) => Call(
            compiler,
            "DeferAlternativeTupleArmFrame",
            code,
            labels
        );
    }

    private static byte[] Fixture(
        bool ordered = false,
        bool wrongRestore = false,
        bool sharedCleanup = false,
        bool earlyHeap = false
    )
    {
        byte[] check = [IsTaggedTuple, F(ordered ? 1 : 4), X(0), U(3), A(3)];
        byte[] init = !ordered || earlyHeap ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] fallback = ordered ? [] : [Label, U(4), Move, Y(1), X(wrongRestore ? 0 : 1), Deallocate, U(2), Jump, F(1)];
        byte[] extraCheck = sharedCleanup ? [ExactEqualTest, F(4), Y(0), A(2)] : [];
        byte[] heap = earlyHeap ? [TestHeap, U(2), U(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            ExactEqualTest, F(3), X(1), A(2), TailExternalCall, U(1), U(0), Label, U(3),
            .. (ordered ? check : []), Allocate, U(2), U(2), .. init, Move, X(1), Y(1),
            .. (ordered ? [] : check), .. heap, GetTupleElement, X(0), U(1), Y(0), .. extraCheck,
            Move, Y(1), X(1), Move, Y(0), X(0), TailExternalCallWithFrame, U(2), U(1), U(2),
            .. fallback, EndCode
        ];
    }
}
