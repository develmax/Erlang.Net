using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedRetryArityCarrierChecks
{
    internal const string TestName = "compiler/beam/reuses-dead-entry-tuple-register-across-arity-retry";
    private const string WrongLayout = "The original tuple must be dead on both paths before its register holds the tested field.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_checked_retry_arity_carrier");
        _ = Call(compiler, "Intern", "checked_tuple");
        _ = Call(compiler, "Intern", "wrapper_name");
        if (Apply(Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(optimized: true))
            || Apply(actual) is not null || Apply(Fixture(readCarrierOnSuccess: true)) is not null
            || Apply(Fixture(readCarrierOnFailure: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseCheckedRetryArityCarrier",
            code,
            5
        );
    }

    private static byte[] Fixture(
        bool optimized = false,
        bool readCarrierOnSuccess = false,
        bool readCarrierOnFailure = false
    )
    {
        byte checkedField = optimized ? X(0) : X(3);
        byte[] fallback = readCarrierOnFailure ? [] : [Move, X(1), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(4), X(0), U(3), A(2),
            GetTupleElement, X(0), U(1), X(2),
            GetTupleElement, X(0), U(2), checkedField,
            ExactEqualTest, F(3), checkedField, I(0),
            Allocate, U(1), U(3),
            Move, readCarrierOnSuccess ? X(0) : X(1), Y(0), Move, X(2), X(0),
            Deallocate, U(1), Return,
            Label, U(3), ExactEqualTest, F(4), X(2), A(3), ExactEqualTest, F(4), checkedField, I(1),
            Move, A(1), X(0), Return,
            Label, U(4), .. fallback, Return, EndCode
        ];
    }
}
