using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalCheckedTupleFieldChecks
{
    internal const string TestName = "compiler/beam/reuses-a-checked-tuple-for-its-terminal-field-equality";

    internal static Task Run()
    {
        byte[] original = Fixture();
        byte[] expected = Fixture(rewritten: true);
        if (Rewrite(original) is not { } result || !result.SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(observesOnFailure: true)) is not null
            || Rewrite(Fixture(usesCarrierAtHeap: true)) is not null
            || Rewrite(Fixture(comparesCarrier: true)) is not null)
        {
            throw new InvalidOperationException("The checked field may replace its tuple only when both outcomes retire it.");
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool rewritten = false,
        bool observesOnFailure = false,
        bool usesCarrierAtHeap = false,
        bool comparesCarrier = false
    )
    {
        byte field = rewritten ? X(3) : X(4);

        return
        [
            .. Start(
                1,
                2,
                1,
                5
            ),
            IsTuple, F(3), X(3), TestArity, F(3), X(3), U(2),
            GetTupleElement, X(3), U(0), field,
            ExactEqualTest, F(3), field, comparesCarrier ? X(3) : X(0),
            TestHeap, U(4), usesCarrierAtHeap ? U(4) : U(3),
            UpdateRecord, A(2), U(3), X(1), X(0), ExtendedListOperand, U(2), U(3), X(2),
            Return,
            Label, U(3), Move, observesOnFailure ? X(3) : X(1), X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            4,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteTerminalCheckedTupleField",
            code,
            body
        );
    }
}
