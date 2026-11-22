using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLiteralJoinCopyOrderChecks
{
    internal const string TestName = "compiler/beam/orders-independent-literal-copies-by-the-join-peer";
    private const string WrongOrder = "Only independent literal join copies may adopt the sibling's order.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_literal_join_copy_order");
        _ = Call(compiler, "Intern", "subject");
        _ = Call(compiler, "LiteralOperand", Term.List(Term.I(1)));
        if (Apply(Fixture()) is not { } ordered || !ordered.SequenceEqual(Fixture(ordered: true))
            || Apply(ordered) is not null || Apply(Fixture(dependent: true)) is not null
            || Apply(Fixture(otherSlot: true)) is not null
            || Apply(Fixture(extraPredecessor: true)) is not null)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OrderLiteralJoinCopies",
            code,
            6
        );
    }

    private static byte[] Fixture(
        bool ordered = false,
        bool dependent = false,
        bool otherSlot = false,
        bool extraPredecessor = false
    )
    {
        byte[] first = [Move, I(1), Y(1)];
        byte[] second = dependent
            ? [Move, Y(1), Y(0)]
            : [Move, ExtendedLiteralOperand, U(0), Y(0)];
        byte[] copies = ordered ? [.. second, .. first] : [.. first, .. second];
        byte[] extra = extraPredecessor ? [InitYRegisters, ExtendedListOperand, U(1), Y(0), IsTuple, F(4), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            Allocate, U(3), U(2), .. extra,
            IsNonemptyList, F(3), X(0),
            Move, X(1), Y(0), Move, X(0), Y(otherSlot ? 2 : 1), Jump, F(4),
            Label, U(3), IsNil, F(5), X(0), .. copies,
            Label, U(4), Move, Y(0), X(0), Deallocate, U(3), Return,
            Label, U(5), Deallocate, U(3), Return, EndCode
        ];
    }
}
