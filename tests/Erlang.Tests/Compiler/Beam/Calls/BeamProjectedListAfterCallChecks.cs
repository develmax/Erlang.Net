using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedListAfterCallChecks
{
    internal const string TestName = "compiler/beam/schedules-an-independent-list-projection-before-saving-a-call-result";
    private const string WrongLayout = "Moving the projection must preserve the nil continuation and the original case error.";

    internal static Task Run()
    {
        if (Rewrite(Layout(scheduled: false)) is not { } result
            || !result.SequenceEqual(Layout(scheduled: true))
            || Rewrite(Layout(scheduled: false, readsOriginalOnNil: true)) is not null
            || Rewrite(result) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Layout(bool scheduled, bool readsOriginalOnNil = false)
    {
        int list = scheduled ? 1 : 0;
        byte[] save = [Move, X(0), Y(0)];
        byte[] projection = [GetTupleElement, Y(1), U(0), X(list)];
        byte[] setup = scheduled ? [.. projection, .. save] : [.. save, .. projection];
        byte[] empty = readsOriginalOnNil ? [] : [Move, Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1),
            ExternalCall, U(1), U(0), .. setup,
            IsNonemptyList, F(3), X(list), GetList, X(list), X(0), X(1),
            Deallocate, U(2), Return,
            Label, U(3), IsNil, F(4), X(list), .. empty, Deallocate, U(2), Return,
            Label, U(4), CaseEnd, X(list),
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
            5,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            1,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteProjectedListAfterCall",
            image,
            body
        );
    }
}
