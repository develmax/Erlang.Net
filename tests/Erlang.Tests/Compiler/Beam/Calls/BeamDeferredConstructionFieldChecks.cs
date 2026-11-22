using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDeferredConstructionFieldChecks
{
    internal const string TestName = "compiler/beam/reprojects-a-retained-construction-field-after-a-call";
    private const string WrongLayout = "A deferred projection must keep the checked carrier and preserve the constructor inputs.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_deferred_construction_field_probe");
        byte[] expected = Fixture(deferred: true);
        if (Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(Fixture(clearCarrier: true)) is not null
            || Rewrite(Fixture(readDestination: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "DeferConstructionFieldPastCalls",
            code,
            5
        );
    }

    private static byte[] Fixture(bool deferred = false, bool clearCarrier = false, bool readDestination = false)
    {
        int width = deferred ? 1 : 2;
        byte carrier = Y(width - 1);
        byte[] beforeCall = deferred ? [] : [GetTupleElement, X(0), U(1), Y(0)];
        byte[] afterCall = deferred ? [GetTupleElement, carrier, U(1), X(0)] : [];
        byte[] clear = clearCarrier ? [InitYRegisters, ExtendedListOperand, U(1), carrier] : [];
        byte[] cons = [PutList, readDestination ? X(0) : X(1), deferred ? X(0) : Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsTaggedTuple, F(3), X(0), U(2), A(1),
            Allocate, U(width), U(1), Move, X(0), carrier, .. beforeCall, .. clear,
            Line, U(1), LocalCall, U(1), F(2), TestHeap, U(5), U(1), Move, X(0), X(1),
            .. afterCall, .. cons, Move, carrier, X(1),
            PutTuple, X(0), ExtendedListOperand, U(2), X(0), X(1), Deallocate, U(width), Return,
            Label, U(3), Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1), TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }
}
