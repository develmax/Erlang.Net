using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalPairListRootsChecks
{
    internal const string TestName = "compiler/beam/roots-terminal-list-tail-before-returned-pair-fields";
    private const string WrongOrder = "Terminal pair fields may change slots only when the reserved list tail remains independent.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_terminal_pair_list_roots");
        _ = Call(compiler, "Intern", "subject");
        _ = Call(compiler, "Intern", "copy");
        if (Apply(Fixture()) is not { } scheduled || !scheduled.SequenceEqual(Fixture(scheduled: true))
            || Apply(scheduled) is not null || Apply(Fixture(extraLive: true)) is not null
            || Apply(Fixture(readsCarrier: true)) is not null || Apply(Fixture(readsScratch: true)) is not null)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ScheduleTerminalPairBeforeListConstruction",
            code,
            3
        );
    }

    private static byte[] Fixture(
        bool scheduled = false,
        bool extraLive = false,
        bool readsCarrier = false,
        bool readsScratch = false
    )
    {
        byte[] tail = [GetTupleElement, Y(0), U(1), X(1)];
        byte[] earlier = scheduled ? tail : [];
        byte[] later = scheduled ? [] : tail;
        byte[] fields = readsCarrier
            ? [X(1), X(2), X(0)]
            : scheduled ? [X(2), X(0)] : [X(1), X(2)];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            IsTuple, F(1), X(0), TestArity, F(1), X(0), U(2),
            IsTuple, F(1), X(1), TestArity, F(1), X(1), U(2),
            Allocate, U(1), U(2), Move, X(1), Y(0), TestHeap, U(readsCarrier ? 9 : 8), U(extraLive ? 2 : 1),
            .. earlier, GetTupleElement, X(0), U(0), X(scheduled ? 2 : 1),
            GetTupleElement, X(0), U(1), X(scheduled ? 0 : 2),
            PutTuple, X(0), ExtendedListOperand, U(fields.Length), .. fields, .. later,
            PutList, X(0), X(1), X(0),
            UpdateRecord, A(3), U(2), Y(0), X(0), ExtendedListOperand, U(2), U(2), X(readsScratch ? 2 : 0),
            Deallocate, U(1), Return, EndCode
        ];
    }
}
