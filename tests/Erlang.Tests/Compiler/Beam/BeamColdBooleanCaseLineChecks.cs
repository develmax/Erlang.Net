using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamColdBooleanCaseLineChecks
{
    internal const string TestName = "compiler/beam/moves-a-cold-boolean-exit-only-with-an-equivalent-line";
    private const string ReadBlock = "ReadColdBooleanCaseBlock";
    private const string BoundaryError = "A cold Boolean failure crossed a line used by the failure.";

    internal static void Run()
    {
        byte[] start = [.. Start(
            1,
            2,
            1,
            1
        ), Return, Label, U(3)];
        byte[] failure = [CaseEnd, X(0), Label, U(4)];
        byte[] last = [Return, EndCode];
        byte[] bare = [.. start, .. failure, .. last];
        byte[] changedLine = [.. start, .. failure, Line, U(1), .. last];
        byte[] explicitLine = [.. start, Line, U(2), .. failure, Line, U(1), .. last];

        if (Find(bare) != 6 || Find(changedLine) is not null || Find(explicitLine) != 7)
        {
            throw new InvalidOperationException(BoundaryError);
        }
    }

    private static int? Find(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
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
        int last = ((System.Collections.IEnumerable)body).Cast<object>().Count() - 1;

        return Call(
            typeof(BeamModuleCompiler),
            ReadBlock,
            body,
            4,
            last,
            last
        ) as int?;
    }
}
