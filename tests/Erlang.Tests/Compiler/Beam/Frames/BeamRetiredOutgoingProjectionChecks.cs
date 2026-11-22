using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetiredOutgoingProjectionChecks
{
    internal const string TestName = "compiler/beam/forwards-retired-outgoing-projections";
    private const string Pass = "ForwardRetiredOutgoingProjections";
    private const string InvalidLayout = "The projected argument must move to X while the dead old frame home is cleared.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retired_outgoing_projection");
        if (Rewrite(compiler, Fixture()) is not { } output
            || !output.SequenceEqual(Fixture(forward: true))
            || Rewrite(compiler, output) is not null
            || Rewrite(compiler, Fixture(beforeReload: [Move, X(1), X(2)])) is not null
            || Rewrite(compiler, Fixture(beforeReload: [TestHeap, U(2), U(1)])) is not null
            || Rewrite(compiler, Fixture(beforeReload: [Label, U(4)])) is not null
            || Rewrite(compiler, Fixture(useHomeAfterCall: true)) is not null)
        {
            throw new InvalidOperationException(InvalidLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, byte[] bytes) =>
        (byte[]?)Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(bytes),
            5
        );

    private static byte[] Fixture(
        bool forward = false,
        byte[]? beforeReload = null,
        bool useHomeAfterCall = false
    )
    {
        byte projected = forward ? X(1) : Y(0);
        byte[] reloadOrClear = forward ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [Move, Y(0), X(1)];
        byte[] next = useHomeAfterCall ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, U(1), U(2),
            Move, X(1), Y(0), ExternalCall, U(2), U(0),
            IsTuple, F(3), X(0), TestArity, F(3), X(0), U(1),
            GetTupleElement, X(0), U(0), projected, .. (beforeReload ?? []), .. reloadOrClear,
            ExternalCall, U(2), U(0), .. next, Deallocate, U(1), Return,
            Label, U(3), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }
}
