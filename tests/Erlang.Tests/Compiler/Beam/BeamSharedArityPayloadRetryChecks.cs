using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedArityPayloadRetryChecks
{
    internal const string TestName = "compiler/beam/reuses-a-shared-arity-payload-on-list-retry";
    private const string WrongLayout = "The shared field must reach every retry edge from an unchanged tuple carrier.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_shared_arity_payload_retry");
        _ = Call(compiler, "Intern", "first");
        _ = Call(compiler, "Intern", "second");
        _ = Call(compiler, "Intern", "third");
        if (Apply(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(shared: true))
            || Apply(changed) is not null || Apply(Fixture(clobbered: true)) is not null
            || Apply(Fixture(unprepared: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseSharedArityPayloadOnRetry",
            code,
            9
        );
    }

    private static byte[] Fixture(bool shared = false, bool clobbered = false, bool unprepared = false)
    {
        byte[] overwrite = clobbered ? [Move, X(0), X(2)] : [];
        byte[] bypass = unprepared ? [IsTaggedTuple, F(8), X(1), U(2), A(2), ExactEqualTest, F(7), X(0), I(0)] : [];
        byte[] repeated = shared ? [] : [GetTupleElement, X(1), U(1), X(2)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            .. bypass, IsTuple, F(8), X(1), SelectTupleArity, X(1), F(8), ExtendedListOperand, U(2), U(2), F(3),
            Label, U(3), GetTupleElement, X(1), U(0), X(3), GetTupleElement, X(1), U(1), X(2),
            SelectValue, X(3), F(8), ExtendedListOperand, U(6), A(2), F(4), A(3), F(5), A(4), F(6),
            Label, U(4), .. overwrite, IsNil, F(7), X(0), Return,
            Label, U(5), IsNonemptyList, F(7), X(0), Return,
            Label, U(6), Move, X(2), X(0), Return,
            Label, U(7), .. repeated, IsNonemptyList, F(8), X(0), Move, X(2), X(0), Return,
            Label, U(8), Return, EndCode
        ];
    }
}
