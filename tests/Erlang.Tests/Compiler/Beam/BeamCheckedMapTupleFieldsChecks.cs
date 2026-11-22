using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedMapTupleFieldsChecks
{
    internal const string TestName = "compiler/beam/coalesces-map-fields-through-a-checked-tuple-pattern";
    private const string WrongLayout = "Grouped map projections exposed a saved field or changed the original rejection.";

    internal static Task Run()
    {
        if (Rewrite(Image(grouped: false)) is not { } rewritten || !rewritten.SequenceEqual(Image(grouped: true))
            || Rewrite(Image(grouped: false, readsSavedOnFailure: true)) is not null
            || Rewrite(Image(grouped: false, initializesSaved: false)) is not null
            || Rewrite(rewritten) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool grouped, bool readsSavedOnFailure = false, bool initializesSaved = true)
    {
        byte[] initial = initializesSaved ? [U(4), Y(0), Y(1), Y(2), Y(3)] : [U(3), Y(1), Y(2), Y(3)];
        byte[] firstFields = grouped
            ? [U(4), A(4), Y(0), A(3), Y(3)]
            : [U(2), A(3), X(0), Move, X(0), Y(3)];
        byte[] secondField = grouped
            ? []
            : [GetMapElements, F(3), ExtendedTypedRegisterOperand, Y(4), U(1), ExtendedListOperand, U(2), A(4), X(0), Move, X(0), Y(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(5), U(1), InitYRegisters, ExtendedListOperand, .. initial, Move, X(0), Y(4),
            IsMap, F(3), Y(4), GetMapElements, F(3), ExtendedTypedRegisterOperand, Y(4), U(1), ExtendedListOperand, .. firstFields,
            IsTuple, F(3), Y(3), TestArity, F(3), Y(3), U(2),
            GetTupleElement, Y(3), U(0), Y(2), GetTupleElement, Y(3), U(1), Y(1),
            .. secondField, GcBifTwo, F(3), U(0), U(0), Y(2), Y(1), X(0),
            Deallocate, U(5), Return,
            Label, U(3), Move, Y(readsSavedOnFailure ? 0 : 4), X(0), Deallocate, U(5), Return,
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
            4,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteCheckedMapTupleFields",
            image,
            body
        );
    }
}
