using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalConstructorArgumentTrimChecks
{
    internal const string TestName = "compiler/beam/trims-a-consumed-terminal-constructor-argument-slot";
    private const string WrongLayout = "The trim must preserve the low root, outgoing argument, and sibling epoch.";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(Fixture(trimmed: true))
            || Rewrite(actual) is not null || Rewrite(Fixture(observeArgument: true)) is not null
            || Rewrite(Fixture(overwriteLow: true)) is not null
            || Rewrite(Fixture(external: true), pure: true) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var compiler = new BeamModuleCompiler("terminal_callback_cons_trim_fixture");
        _ = Call(compiler, "Intern", BeamOpcodeEdgeNames.Run);
        foreach (bool low in new[] { false, true })
        {
            if (Callback(compiler, CallbackFixture(low)) is not { } compacted
                || !compacted.SequenceEqual(CallbackFixture(low, trimmed: true))
                || Callback(compiler, CallbackFixture(low, otherRead: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        return Task.CompletedTask;
    }

    private static byte[]? Callback(BeamModuleCompiler compiler, byte[] fixture) => (byte[]?)Call(
        compiler,
        "TrimTerminalCallbackCons",
        new ReadOnlyMemory<byte>(fixture),
        5
    );

    private static byte[] CallbackFixture(bool low, bool trimmed = false, bool otherRead = false)
    {
        byte keep = low ? Y(0) : Y(1);
        byte retire = low ? Y(1) : Y(0);
        byte[] move = low && trimmed ? [Move, Y(0), Y(1)] : [];
        byte[] trim = trimmed ? [TrimFrame, U(1), U(1)] : [];
        byte head = trimmed ? Y(0) : keep;

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            AllocateHeap, U(2), U(0), U(2), Move, X(0), retire, Move, X(1), keep,
            MakeFun, U(0), X(0), ExtendedListOperand, U(0), LocalCall, U(2), F(2),
            Move, X(0), X(1), Move, retire, X(0), .. move, .. trim, Line, U(1), LocalCall, U(2), F(2),
            TestHeap, U(2), U(1), PutList, head, otherRead ? retire : X(0), X(0),
            Deallocate, U(trimmed ? 1 : 2), Return, EndCode
        ];
    }

    private static byte[] Fixture(
        bool trimmed = false,
        bool observeArgument = false,
        bool overwriteLow = false,
        bool external = false
    )
    {
        int width = trimmed ? 3 : 4;
        byte[] trim = trimmed ? [Move, Y(0), Y(2), TrimFrame, U(1), U(3)] : [];
        byte[] overwrite = overwriteLow ? [Move, X(0), Y(0)] : [];
        byte[] call = external ? [ExternalCall, U(1), U(0)] : [LocalCall, U(1), F(5)];
        byte low = Y(trimmed ? 1 : 0);
        byte other = Y(trimmed ? 0 : 1);
        byte high = Y(trimmed ? 2 : 3);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(3), X(0), Allocate, U(4), U(2),
            InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2), Move, X(1), Y(3),
            LocalCall, U(1), F(5),
            Move, X(0), Y(1), Move, X(0), Y(2), Move, Y(2), X(0), .. trim, Line, U(1), .. call, .. overwrite,
            TestHeap, U(5), U(1),
            PutTuple, X(0), ExtendedListOperand, U(4), ExtendedTypedRegisterOperand, low, U(0),
            other, high, observeArgument ? Y(2) : X(0),
            Deallocate, U(width), Return,
            Label, U(3), Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1), TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return,
            .. Start(
                4,
                5,
                2,
                1
            ),
            Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes, bool pure = false)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
            2,
            code
        );
        var stack = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordStack = (start, end, slot) => stack.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            recordStack
        );
        Func<int, bool> isPureImport = _ => pure;

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteConsumedTerminalConstructorArgument",
            code,
            body,
            stack,
            isPureImport
        );
    }
}
