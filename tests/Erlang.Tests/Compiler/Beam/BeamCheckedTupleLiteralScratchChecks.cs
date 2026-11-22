using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedTupleLiteralScratchChecks
{
    internal const string TestName = "compiler/beam/reuses-scratch-across-checked-tuple-literals";
    private const string WrongLayout = "Only a dead scratch can be shared across two checked tuple fields.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_checked_tuple_literal_scratch");
        _ = Call(compiler, "Intern", "outer");
        _ = Call(compiler, "Intern", "field");
        if (Apply(Fixture()) is not { } current || !current.SequenceEqual(Fixture(shared: true))
            || Apply(current) is not null || Apply(Fixture(laterScratch: 2)) is not null
            || Apply(Fixture(laterScratch: 3)) is not null || Apply(Fixture(extraHeapRoot: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseCheckedTupleLiteralScratch",
            code,
            4
        );
    }

    private static byte[] Fixture(bool shared = false, int? laterScratch = null, bool extraHeapRoot = false)
    {
        byte next = X(shared ? 2 : 3);
        byte[] laterRead = laterScratch is int scratch ? [Move, X(scratch), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(3), X(0), U(3), A(2),
            GetTupleElement, X(0), U(1), X(2), ExactEqualTest, F(3), X(2), A(3),
            GetTupleElement, X(0), U(2), next, ExactEqualTest, F(3), next, I(0),
            TestHeap, U(2), U(extraHeapRoot ? 3 : 2), .. laterRead, Return,
            Label, U(3), Move, X(1), X(0), Return, EndCode
        ];
    }
}
