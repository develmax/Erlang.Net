using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostNames;

namespace Erlang.Tests;

internal static class BeamBinaryErrorPriorityHostChecks
{
    internal static Task IntegerSizeFirst()
    {
        RequireReason(
            IntegerAtom,
            Term.A(BeamBinaryErrorPriorityHostNames.BadInteger),
            new Integer(BigInteger.One << 64),
            ErlangErrorReasons.SystemLimit
        );
        RequireReason(
            IntegerAtom,
            Term.A(BeamBinaryErrorPriorityHostNames.BadInteger),
            Term.I(-1),
            ErlangErrorReasons.BadArgument
        );

        return Task.CompletedTask;
    }

    internal static Task BinarySizeFirst()
    {
        RequireReason(
            BinaryAtom,
            Term.A(BeamBinaryErrorPriorityHostNames.BadBinary),
            new Integer(BigInteger.One << 64),
            ErlangErrorReasons.SystemLimit
        );
        RequireReason(
            BinaryAtom,
            Term.A(BeamBinaryErrorPriorityHostNames.BadBinary),
            Term.I(-1),
            ErlangErrorReasons.BadArgument
        );

        return Task.CompletedTask;
    }

    internal static Task ZeroWidthChecksType()
    {
        RequireReason(
            IntegerAtom,
            Term.A(BeamBinaryErrorPriorityHostNames.BadInteger),
            Term.I(0),
            ErlangErrorReasons.BadArgument
        );
        object state = State(Term.I(-19), Term.I(0));
        BitString result = Create(StateCode(), state, Operands(IntegerAtom));
        Require(result.BitLength == 0);

        return Task.CompletedTask;
    }

    internal static Task LaterSizeBeforeEarlierValue()
    {
        object state = State(
            Term.A(BeamBinaryErrorPriorityHostNames.BadInteger),
            Term.I(1),
            Term.I(0),
            new Integer(BigInteger.One << 64)
        );
        Array operands = Ops(
            Op(Failed, 0),
            Op(Unsigned, 0),
            Op(Unsigned, 4),
            Op(Unsigned, 1),
            Op(X, 0),
            Op(Count, 12),
            Op(AtomKind, IntegerAtom),
            Op(Unsigned, 0),
            Op(Unsigned, 1),
            Op(NilKind, 0),
            Op(X, 0),
            Op(X, 1),
            Op(AtomKind, IntegerAtom),
            Op(Unsigned, 1),
            Op(Unsigned, 1),
            Op(NilKind, 0),
            Op(X, 2),
            Op(X, 3)
        );
        Expect(ErlangErrorReasons.SystemLimit, () => Create(StateCode(), state, operands));

        return Task.CompletedTask;
    }

    internal static Task BinaryAllTypeStaysFirstPass()
    {
        object state = State(
            Term.A(BeamBinaryErrorPriorityHostNames.BadBinary),
            Term.A(BeamBinaryErrorPriorityHostNames.All),
            Term.I(0),
            new Integer(BigInteger.One << 64)
        );
        Expect(ErlangErrorReasons.BadArgument, () => Create(StateCode(), state, BinaryAllThenInteger()));
        object single = State(Term.A(BeamBinaryErrorPriorityHostNames.BadBinary), Term.A(BeamBinaryErrorPriorityHostNames.All));
        Expect(ErlangErrorReasons.BadArgument, () => Create(StateCode(), single, Operands(BinaryAtom)));

        return Task.CompletedTask;
    }

    internal static Task BinaryAllUnitAfterLaterSize()
    {
        object state = State(
            new BitString(new byte[] { 0xa0 }, 3),
            Term.A(BeamBinaryErrorPriorityHostNames.All),
            Term.I(0),
            new Integer(BigInteger.One << 64)
        );
        Expect(ErlangErrorReasons.SystemLimit, () => Create(StateCode(), state, BinaryAllThenInteger()));

        return Task.CompletedTask;
    }

    private static Array BinaryAllThenInteger() => Ops(
        Op(Failed, 0),
        Op(Unsigned, 0),
        Op(Unsigned, 4),
        Op(Unsigned, 1),
        Op(X, 0),
        Op(Count, 12),
        Op(AtomKind, BinaryAtom),
        Op(Unsigned, 0),
        Op(Unsigned, 8),
        Op(NilKind, 0),
        Op(X, 0),
        Op(X, 1),
        Op(AtomKind, IntegerAtom),
        Op(Unsigned, 1),
        Op(Unsigned, 1),
        Op(NilKind, 0),
        Op(X, 2),
        Op(X, 3)
    );

    private static object StateCode() => Code(BeamBinaryHostBytes.Image());

    private static object State(params Term[] values)
    {
        object state = New(RegisterState);
        for (int index = 0; index < values.Length; index++)
            Call(
                state,
                Write,
                index,
                values[index]
            );

        return state;
    }

    private static Array Operands(int kind) => Ops(
        Op(Failed, 0),
        Op(Unsigned, 0),
        Op(Unsigned, 2),
        Op(Unsigned, 1),
        Op(X, 0),
        Op(Count, 6),
        Op(AtomKind, kind),
        Op(Unsigned, 0),
        Op(Unsigned, 1),
        Op(NilKind, 0),
        Op(X, 0),
        Op(X, 1)
    );

    private static BitString Create(object code, object state, Array operands)
    {
        Validate(
            code,
            CreateName,
            CreateOpcode,
            6,
            operands
        );

        return (BitString)Call(
            Type(Execution),
            CreateMethod,
            null!,
            code,
            state,
            operands
        );
    }

    private static void RequireReason(
        int kind,
        Term value,
        Term size,
        string reason
    )
        => Expect(reason, () => Create(StateCode(), State(value, size), Operands(kind)));

    private static void Expect(string reason, Action action)
    {
        try
        {
            action();
        }
        catch (ErlangException error)
        {
            Require(error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(reason)));

            return;
        }
        throw new InvalidOperationException(BeamBinaryErrorPriorityHostNames.Failure);
    }
}
