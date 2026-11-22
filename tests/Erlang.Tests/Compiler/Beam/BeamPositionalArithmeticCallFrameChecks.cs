using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPositionalArithmeticCallFrameChecks
{
    internal const string TestName = "compiler/beam/moves-positional-arithmetic-before-checked-call-frame";
    private const string WrongFrame = "Arithmetic selector arguments must keep their physical positions before checked-result frame allocation.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("positional_arithmetic_probe");
        _ = Call(compiler, "Intern", "ok");
        _ = Call(
            compiler,
            "Import",
            "erlang",
            "+",
            2
        );
        byte[] original = Fixture();
        byte[]? compact = Apply(original);
        if (compact is null || !compact.SequenceEqual(Fixture(compacted: true))
            || Apply(compact) is not null || Apply(Fixture(observeOriginal: true)) is not null
            || Apply(Fixture(differentRestore: true)) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        return Task.CompletedTask;

        byte[]? Apply(byte[] instructions) => (byte[]?)Call(
            compiler,
            "PlacePositionalArithmeticBeforeCallFrame",
            (ReadOnlyMemory<byte>)instructions,
            6
        );
    }

    private static byte[] Fixture(bool compacted = false, bool observeOriginal = false, bool differentRestore = false)
    {
        byte width = U(compacted ? 2 : 3);
        byte[] arguments = compacted
            ?
            [
                GcBifTwo, F(0), U(3), U(0), X(0), I(1), X(0),
                GcBifTwo, F(0), U(3), U(0), X(2), I(2), X(2),
                Allocate, width, U(3),
                InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)
            ]
            :
            [
                Allocate, width, U(3),
                Move, X(2), Y(0), Move, X(1), Y(1), Move, X(0), Y(2),
                GcBifTwo, F(0), U(0), U(0), Y(2), I(1), X(0), Move, X(0), Y(2),
                GcBifTwo, F(0), U(0), U(0), Y(0), I(2), X(0), Move, X(0), X(2),
                Move, Y(2), X(0), Move, Y(differentRestore ? 0 : 1), X(1)
            ];
        byte firstField = Y(compacted ? 1 : 2);
        byte secondField = Y(compacted ? 0 : 1);
        byte[] extra = observeOriginal ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            .. arguments,
            LocalCall, U(3), F(5),
            IsTaggedTuple, F(3), X(0), U(2), A(2),
            GetTupleElement, X(0), U(1), X(1),
            GetTupleElement, X(1), U(0), firstField,
            GetTupleElement, X(1), U(1), secondField,
            Move, firstField, X(0), .. extra,
            Label, U(3), Deallocate, width, Return,
            .. Start(
                4,
                5,
                2,
                3
            ),
            Return, EndCode
        ];
    }
}
