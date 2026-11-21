using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDeadTupleProjectionChecks
{
    internal const string TestName = "compiler/beam/removes-projections-without-a-live-successor";
    private const string UnexpectedRewrite = "A checked field projection changed an observable or implicit register read.";
    private const int Function = 1;
    private const int Entry = 2;
    private const int Retry = 3;
    private const int Invalid = 4;
    private const int Labels = 5;
    private const int Arity = 2;

    internal static Task Run()
    {
        if (Rewrite(Body()) is not { } changed || !changed.SequenceEqual(Body(omit: true))
            || Rewrite(Body(readOnRetry: true)) is not null
            || Rewrite(Body(rootAtHeap: true)) is not null)
        {
            throw new InvalidOperationException(UnexpectedRewrite);
        }

        return Task.CompletedTask;
    }

    private static byte[] Body(bool omit = false, bool readOnRetry = false, bool rootAtHeap = false)
    {
        byte[] projection = omit ? [] : [GetTupleElement, X(0), U(1), X(2)];
        byte[] heap = rootAtHeap ? [TestHeap, U(2), U(3)] : [];
        byte[] retry = readOnRetry ? [Move, X(2), X(0)] : [Move, A(1), X(0)];

        return
        [
            .. Start(
                Function,
                Entry,
                2,
                Arity
            ),
            IsTuple, F(Invalid), X(0), TestArity, F(Invalid), X(0), U(2),
            .. projection, .. heap,
            IsNil, F(Retry), X(1), Move, A(2), X(0), Return,
            Label, U(Retry), .. retry, Return,
            Label, U(Invalid), Move, A(1), X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            Labels,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteDeadTupleProjections",
            code,
            body
        );
    }
}
