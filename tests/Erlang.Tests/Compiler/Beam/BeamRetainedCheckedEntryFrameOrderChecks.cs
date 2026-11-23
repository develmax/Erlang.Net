using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedCheckedEntryFrameOrderChecks
{
    internal const string TestName = "compiler/beam/orders-retained-checked-entry-frame-slots";
    private const string WrongLayout = "Slot reversal must follow the entry identities and preserve both continuations.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retained_checked_entry_frame_order");
        _ = Call(compiler, "Intern", "checked_tuple");
        foreach (bool carrierLives in new[] { false, true })
        {
            if (Apply(Fixture(carrierLives)) is not { } changed
                || !changed.SequenceEqual(Fixture(carrierLives, ordered: true))
                || Apply(changed) is not null
                || Apply(Fixture(carrierLives, unrelatedSave: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        foreach (bool excludesNil in new[] { false, true })
        {
            if (OrderCopies(ArityFixture(excludesNil)) is not { } ordered
                || !ordered.AsSpan().SequenceEqual(ArityFixture(excludesNil, ordered: true))
                || OrderCopies(ordered) is not null
                || OrderCopies(ArityFixture(excludesNil, usesSelector: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        return Task.CompletedTask;

        byte[]? OrderCopies(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OrderAritySelectedFrameRootCopies",
            code,
            6
        );

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OrderRetainedCheckedEntryFrameSlots",
            code,
            6
        );
    }

    private static byte[] ArityFixture(bool excludesNil, bool ordered = false, bool usesSelector = false)
    {
        byte[] first = [Move, X(1), Y(1)];
        byte[] second = [Move, X(usesSelector ? 2 : 3), Y(0)];
        byte[] nil = excludesNil ? [ExactNotEqualTest, F(5), X(3), A(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(5), X(0), SelectTupleArity, X(0), F(5), ExtendedListOperand, U(2), U(3), F(3),
            Label, U(3),
            GetTupleElement, X(0), U(0), X(2), GetTupleElement, X(0), U(1), X(3),
            GetTupleElement, X(0), U(2), X(0),
            SelectValue, X(2), F(5), ExtendedListOperand, U(2), A(1), F(4),
            Label, U(4), .. nil, IsTaggedTuple, F(5), X(1), U(2), A(2), Allocate, U(2), U(4),
            .. (ordered ? second : first), .. (ordered ? first : second),
            Line, U(1), ExternalCall, U(1), U(0), TestHeap, U(4), U(1),
            PutTuple, X(0), ExtendedListOperand, U(3), Y(0), Y(1), X(0), Deallocate, U(2), Return,
            Label, U(5), Move, X(1), X(0), Return, EndCode
        ];
    }

    private static byte[] Fixture(bool carrierLives, bool ordered = false, bool unrelatedSave = false)
    {
        int first = carrierLives ? 0 : 1;
        int width = 3 - first;
        byte test = carrierLives ? X(3) : X(0);
        byte field = Slot(width - 1);
        byte outgoing = Slot(1 - first);
        byte[] saves = Enumerable.Range(0, width)
            .SelectMany(slot => new byte[]
            {
                Move,
                X(unrelatedSave && slot == 0 ? 2 : first + (ordered ? width - 1 - slot : slot)),
                Y(slot)
            }).ToArray();

        byte[] prepare = carrierLives
            ?
            [
                TestHeap, U(3), U(1), PutTuple, X(1), ExtendedListOperand, U(2), field, I(0),
                Move, outgoing, X(2), Move, X(0), outgoing, Move, X(1), X(0), Move, X(2), X(1),
                InitYRegisters, ExtendedListOperand, U(1), field
            ]
            : [Move, outgoing, X(1), Move, X(0), outgoing, Swap, X(0), X(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(4), X(0), U(3), A(2),
            GetTupleElement, X(0), U(1), X(2), GetTupleElement, X(0), U(2), test,
            ExactEqualTest, F(3), test, I(0),
            Allocate, U(width), U(3), .. saves,
            Move, X(2), X(0), Line, U(1), ExternalCall, U(1), U(0),
            .. prepare, Line, U(1), ExternalCall, U(2), U(1),
            ExactEqualTest, F(5), X(0), A(1),
            Move, carrierLives ? Slot(0) : field, X(0),
            InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(width - 1),
            Deallocate, U(width), Return,
            Label, U(5), Move, outgoing, X(0), Deallocate, U(width), Return,
            Label, U(3), ExactEqualTest, F(4), test, I(1), Move, X(1), X(0), Return,
            Label, U(4), Move, X(1), X(0), Return,
            EndCode
        ];

        byte Slot(int original) => Y(ordered ? width - 1 - original : original);
    }
}
