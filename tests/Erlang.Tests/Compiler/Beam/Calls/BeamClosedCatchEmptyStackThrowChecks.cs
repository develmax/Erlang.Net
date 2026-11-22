using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosedCatchEmptyStackThrowChecks
{
    internal const string TestName = "compiler/beam/tails-an-empty-stack-throw-after-the-catch-closes";

    internal static void Run()
    {
        byte[] expected = Body(terminal: true);
        if (Rewrite(Body(terminal: false)) is not { } actual || !expected.SequenceEqual(actual)
            || Rewrite(expected) is not null
            || Rewrite(Body(terminal: false, trace: X(1))) is not null
            || Rewrite(Body(terminal: false, bypassCatch: true)) is not null
            || Rewrite(Body(terminal: false, raiseUnderCatch: true)) is not null
            || Rewrite(Body(terminal: false, trim: true)) is not null)
        {
            throw new InvalidOperationException(
                "Only a same-frame throw with explicit empty trace after the owned catch close can leave with the call."
            );
        }
    }

    private static byte[] Body(
        bool terminal,
        byte trace = 2,
        bool bypassCatch = false,
        bool raiseUnderCatch = false,
        bool trim = false
    )
    {
        byte[] bypass = bypassCatch ? [ExactEqualTest, F(4), X(0), A(1)] : [];
        byte[] close = [Label, U(3), CatchEnd, Y(0)];
        byte[] protectedCall = raiseUnderCatch ? [] : [DynamicApply, U(0)];
        byte[] resize = trim ? [TrimFrame, U(1), U(1)] : [];
        byte[] call = terminal
            ? [TailExternalCallWithFrame, U(3), U(0), U(2)]
            : [ExternalCall, U(3), U(0)];
        byte[] release = [Label, U(5), Deallocate, U(trim ? 1 : 2), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ), Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1), .. bypass,
            CatchEnter, Y(0), F(3), .. protectedCall,
            .. (raiseUnderCatch ? [] : close), .. resize, ExactNotEqualTest, F(5), X(0), A(1), Label, U(4),
            Move, X(0), X(1), Move, trace, X(2), Move, A(2), X(0), Line, U(1), .. call,
            .. release, .. (raiseUnderCatch ? close : []), EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
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
            "RewriteClosedCatchEmptyStackThrow",
            code,
            body,
            0,
            2
        );
    }
}
