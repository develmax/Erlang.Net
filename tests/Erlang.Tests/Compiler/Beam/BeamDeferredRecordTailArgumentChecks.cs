using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDeferredRecordTailArgumentChecks
{
    internal const string TestName = "compiler/beam/defers-checked-record-fields-through-guards-to-tail-arguments";
    private const string WrongLayout = "A guarded tail projection changed a fallback or retained the early record-field reads.";

    internal static Task Run()
    {
        if (Rewrite(Image(deferred: false)) is not { } candidate || !candidate.SequenceEqual(Image(deferred: true))
            || Rewrite(Image(deferred: false, earlyCarrierRead: true)) is not null
            || Rewrite(Image(deferred: false, unownedTail: true)) is not null
            || Rewrite(candidate) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool deferred, bool earlyCarrierRead = false, bool unownedTail = false)
    {
        byte[] prefix = unownedTail ? [IsTuple, F(3), X(0)] : [];
        byte[] read = earlyCarrierRead ? [Move, Y(2), X(0)] : [];
        byte[] fields = deferred ? []
            : [GetTupleElement, Y(2), U(2), Y(0), GetTupleElement, Y(2), U(3), Y(2)];
        byte[] arguments = deferred
            ? [GetTupleElement, Y(2), U(3), X(1), GetTupleElement, Y(2), U(2), X(0)]
            : [Move, Y(2), X(1), Move, Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(3), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            Move, X(0), Y(2), .. prefix, IsTaggedTuple, F(4), Y(2), U(4), A(2),
            GetTupleElement, Y(2), U(1), Y(1), .. fields,
            IsAtom, F(5), Y(1), .. read,
            Label, U(3), .. arguments, Line, U(1), TailExternalCallWithFrame, U(2), U(0), U(3),
            Label, U(4), Move, Y(2), X(0), Deallocate, U(3), Return,
            Label, U(5), Move, A(3), X(0), Deallocate, U(3), Return,
            EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteCheckedRecordTailArguments",
            image,
            body
        );
    }
}
