using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamEntryFieldArgumentChecks
{
    internal const string TestName = "compiler/beam/keeps-the-original-tuple-root-through-entry-allocation";
    private const string RewriteMethod = "RewriteEntryFieldArgument";
    private const string InvalidHandoff = "Moving the field argument changed its frame roots or an earlier read.";

    internal static void Run()
    {
        byte[] original = Body(delayed: false, additionalRead: false, retainedTuple: true);
        byte[] expected = Body(delayed: true, additionalRead: false, retainedTuple: true);
        byte[] read = Body(delayed: false, additionalRead: true, retainedTuple: true);
        byte[] lost = Body(delayed: false, additionalRead: false, retainedTuple: false);
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(read) is not null || Rewrite(lost) is not null)
        {
            throw new InvalidOperationException(InvalidHandoff);
        }
    }

    private static byte[] Body(bool delayed, bool additionalRead, bool retainedTuple)
    {
        int original = retainedTuple ? 2 : 3;
        byte[] projection = [GetTupleElement, X(original), U(0), X(delayed ? 0 : 3)];
        byte[] first = delayed ? [] : projection;
        byte[] arguments = delayed ? projection : [Move, X(3), X(0)];
        byte[] save = additionalRead ? [Move, X(3), Y(0)] : [Move, X(1), Y(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            .. first, Allocate, U(1), U(delayed ? 3 : 4),
            .. save, .. arguments, Line, U(1), ExternalCall, U(2), U(0),
            Deallocate, U(1), Return, EndCode
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
