using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedPairCallReturnsChecks
{
    internal const string TestName = "compiler/beam/trims-checked-pair-return-after-last-outgoing-load";
    private const string WrongFrame = "The checked pair success may trim after its final outgoing load while preserving the fallback frame.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("checked_pair_return_probe");
        _ = Call(compiler, "Intern", "ok");
        byte[] input = Fixture();
        byte[]? compact = Rewrite(input, _ => true);
        if (compact is null || !compact.SequenceEqual(Fixture(compacted: true))
            || Rewrite(compact, _ => true) is not null || Rewrite(input, _ => false) is not null
            || Rewrite(Fixture(retainsSecondField: true), _ => true) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(byte[] code, Func<int, bool> hasPair) => (byte[]?)Call(
            compiler,
            "RewriteCheckedPairCallReturns",
            (ReadOnlyMemory<byte>)code,
            6,
            hasPair
        );
    }

    private static byte[] Fixture(bool compacted = false, bool retainsSecondField = false)
    {
        byte tuple = X(compacted ? 0 : 1);
        byte[] annotation = compacted ? [ExtendedTypedRegisterOperand, X(0), U(1)] : [X(0)];
        byte[] trim = compacted ? [TrimFrame, U(1), U(1)] : [];
        byte[] terminal = compacted ? [Deallocate, U(1), Return] : [];
        byte result = Y(compacted || retainsSecondField ? 0 : 1);

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, U(2), U(0), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            LocalCall, U(0), F(5),
            IsTaggedTuple, F(3), .. annotation, U(2), A(2),
            GetTupleElement, X(0), U(1), tuple,
            GetTupleElement, tuple, U(0), Y(1),
            GetTupleElement, tuple, U(1), Y(0),
            Line, U(0),
            GcBifOne, F(0), U(0), U(0), Y(0), X(0),
            ExternalCall, U(1), U(1),
            Move, Y(0), X(1), .. trim,
            Line, U(0),
            ExternalCall, U(2), U(2), TestHeap, U(6), U(1),
            PutTuple, X(0), ExtendedListOperand, U(2), result, X(0),
            PutTuple, X(0), ExtendedListOperand, U(2), A(2), X(0),
            .. terminal, Label, U(3), Deallocate, U(2), Return,
            .. Start(
                4,
                5,
                2,
                0
            ),
            Move, A(2), X(0), Return, EndCode
        ];
    }
}
