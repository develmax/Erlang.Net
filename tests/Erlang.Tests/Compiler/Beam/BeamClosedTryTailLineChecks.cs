using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosedTryTailLineChecks
{
    internal const string TestName = "compiler/beam/omits-local-tail-line-after-a-dominating-try-end";
    private const string Failure = "The local tail line crossed a protected, annotated, or independently entered path.";

    internal static Task Run()
    {
        const int info = 1;
        const int entry = 2;
        const int handler = 3;
        const int tail = 4;
        byte[] prefix =
        [
            .. Start(
                info,
                entry,
                2,
                1
            ),
            Allocate, U(1), U(1), TryEnter, Y(0), F(handler)
        ];
        byte[] normal = [TryEnd, Y(0), Label, U(tail), Line, U(1), TailLocalCallWithFrame, U(1), F(entry), U(1)];
        byte[] failed = [Label, U(handler), TryCase, Y(0), Deallocate, U(1), Return, EndCode];
        if (Count([.. prefix, .. normal, .. failed]) != 1
            || Count([.. prefix, ExactEqualTest, F(tail), X(0), A(3), .. normal, .. failed]) != 0
            || Count([.. prefix, .. normal, Label, U(handler), TryCase, Y(0), Jump, F(tail), EndCode]) != 0
            || Count([.. prefix, TryEnter, Y(0), F(handler), .. normal, .. failed]) != 0
            || Count([.. prefix, DebugLine, A(1), U(1), U(1), U(1), .. normal, .. failed]) != 0
            || Count([
                .. prefix, TryEnd, Y(0), Line, U(1), TailExternalCallWithFrame, U(1), U(0), U(1), .. failed
            ]) != 0)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[] caught =
        [
            .. Start(
                info,
                entry,
                2,
                1
            ),
            Allocate, U(1), U(1), CatchEnter, Y(0), F(handler)
        ];
        byte[] close = [ExternalCall, U(1), U(0), Label, U(handler), CatchEnd, Y(0)];
        byte[] self = [Label, U(tail), Line, U(1), TailLocalCallWithFrame, U(1), F(entry), U(1), EndCode];
        if (Count([.. caught, .. close, .. self]) != 1
            || Count([.. caught, ExactEqualTest, F(handler), X(0), A(3), .. close, .. self]) != 0
            || Count([.. caught, ExactEqualTest, F(tail), X(0), A(3), .. close, .. self]) != 0
            || Count([.. caught, .. close, Line, U(1), TailExternalCallWithFrame, U(1), U(0), U(1), EndCode]) != 0)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static int Count(byte[] instructions)
    {
        const int labels = 5;
        ReadOnlyMemory<byte> code = instructions;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            labels,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            6,
            0,
            null,
            null
        );

        return ((HashSet<int>?)Call(
            typeof(BeamModuleCompiler),
            "ReadClosedHandlerTailCallLines",
            body,
            0,
            ((IEnumerable)body).Cast<object>().Count() - 1,
            null
        ))?.Count ?? 0;
    }
}
