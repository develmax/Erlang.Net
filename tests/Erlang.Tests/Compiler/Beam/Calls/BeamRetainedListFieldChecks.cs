using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedListFieldChecks
{
    internal const string TestName = "compiler/beam/sinks-a-retained-tuple-field-past-a-recursive-list-call";
    private const string RewriteMethod = "RewriteRetainedListField";
    private const string ProjectionError = "The delayed list head did not keep its checked tuple alive.";

    internal static void Run()
    {
        byte[] original = Body(delayed: false, slot: Y(0), tailInput: X(2));
        byte[] expected = Body(delayed: true, slot: Y(0), tailInput: X(2));
        byte[] differentSlot = Body(delayed: false, slot: Y(1), tailInput: X(2));
        byte[] overwrittenTail = Body(delayed: false, slot: Y(0), tailInput: X(3));
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(differentSlot) is not null || Rewrite(overwrittenTail) is not null)
        {
            throw new InvalidOperationException(ProjectionError);
        }
    }

    private static byte[] Body(bool delayed, byte slot, byte tailInput)
    {
        byte[] frame = delayed
            ? [Allocate, U(1), U(3), Move, X(1), Y(0)]
            : [GetTupleElement, X(1), U(0), X(3), Allocate, U(1), U(4), Move, X(3), Y(0)];
        byte[] forward = delayed ? [GetTupleElement, Y(0), U(0), X(1), PutList, X(1), X(0), X(0)] : [PutList, slot, X(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ), .. frame, Move, tailInput, X(0), Line, U(1), LocalCall, U(1), F(2),
            TestHeap, U(2), U(1), .. forward, Deallocate, U(1), Return, EndCode
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
            RewriteMethod,
            code,
            body
        );
    }
}
