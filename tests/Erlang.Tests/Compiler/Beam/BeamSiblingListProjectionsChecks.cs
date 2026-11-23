using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSiblingListProjectionsChecks
{
    internal const string TestName = "compiler/beam/schedules-checked-tuple-list-projections";
    private const string WrongLayout = "Only independent field and list projections on the same successful test edge can exchange placement.";

    internal static Task Run()
    {
        foreach (bool headNeeded in new[] { false, true })
        {
            if (Rewrite(Layout(headNeeded, scheduled: false)) is not { } result
                || !result.SequenceEqual(Layout(headNeeded, scheduled: true))
                || Rewrite(Layout(headNeeded, scheduled: false, aliased: true)) is not null
                || Rewrite(Layout(headNeeded, scheduled: false, differentFailure: true)) is not null
                || Rewrite(result) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        return Task.CompletedTask;
    }

    private static byte[] Layout(
        bool headNeeded,
        bool scheduled,
        bool aliased = false,
        bool differentFailure = false
    )
    {
        byte[] split = headNeeded ? [GetList, X(1), X(3), X(1)] : [GetTail, X(1), X(1)];
        byte field = aliased ? X(1) : X(2);
        byte[] projection = [GetTupleElement, X(0), U(1), field];
        byte[] middle = scheduled ? [.. projection, .. split] : [.. split, .. projection];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            GetTupleElement, X(0), U(0), X(1), IsNonemptyList, F(1), X(1),
            .. middle, IsNonemptyList, differentFailure ? F(2) : F(1), field, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteSiblingListProjections",
            image,
            body
        );
    }
}
