using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamUnusedMapFieldPresenceChecks
{
    internal const string TestName = "compiler/beam/uses-map-presence-for-unused-checked-key";
    private const string Failure = "Map presence lowering changed a live output, failure value, or frame root.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_unused_map_field_probe");
        _ = Call(compiler, "Intern", "required");
        _ = Call(compiler, "Intern", "retained");
        byte[] lookup = [GetMapElements, F(3), Y(0), ExtendedListOperand, U(4), A(2), Y(1), A(3), Y(2)];
        byte[] presence =
        [
            HasMapFields, F(3), Y(0), ExtendedListOperand, U(1), A(2),
            GetMapElements, F(3), Y(0), ExtendedListOperand, U(2), A(3), Y(2)
        ];
        byte[] liveOutput = [Move, Y(2), X(0)];
        byte[] bothOutputs = [TestHeap, U(3), U(0), PutTuple, X(0), ExtendedListOperand, U(2), Y(1), Y(2)];

        if (Rewrite(Fixture(lookup, liveOutput)) is not { } result
            || !result.SequenceEqual(Fixture(presence, liveOutput))
            || Rewrite(Fixture(presence, liveOutput)) is not null
            || Rewrite(Fixture(lookup, bothOutputs)) is not null
            || Rewrite(Fixture(lookup, liveOutput, initialized: false)) is not null
            || Rewrite(Fixture(lookup, liveOutput, failureSource: 1)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "EmitUnusedMapFieldPresence",
            code,
            5
        );
    }

    private static byte[] Fixture(
        byte[] lookup,
        byte[] after,
        bool initialized = true,
        int failureSource = 0
    ) =>
    [
        .. Start(
            1,
            2,
            1,
            1
        ),
        Allocate, U(3), U(1),
        InitYRegisters, ExtendedListOperand, U(initialized ? 2 : 1),
        .. (initialized ? new byte[] { Y(1), Y(2) } : [Y(2)]),
        Move, X(0), Y(0),
        IsMap, F(3), Y(0),
        .. lookup,
        Jump, F(4),
        Label, U(3),
        Move, Y(failureSource), X(0),
        BadMatch, X(0),
        Label, U(4),
        .. after,
        Deallocate, U(3), Return, EndCode
    ];
}
