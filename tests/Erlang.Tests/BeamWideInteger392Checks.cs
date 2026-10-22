using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWideInteger392Checks
{
    private static object U(int value) => Op(BeamBinaryHostNames.Unsigned, value);

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static Array Commands(int size = 128, int unit = 1)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            Op(BeamBinaryHostNames.Count, 9),
            Op(BeamBinaryHostNames.AtomKind, BeamBinaryHostNames.EnsureAtom),
            U(128),
            U(1),
            Op(BeamBinaryHostNames.AtomKind, BeamBinaryHostNames.IntegerAtom),
            U(2),
            Op(BeamBinaryHostNames.NilKind, 0),
            U(size),
            U(unit),
            X(1)
        );

    private static object State(BitString source, out object context)
    {
        object state = New(BeamBinaryHostNames.RegisterState);
        context = New(BeamBinaryHostNames.MatchContext, source);
        Call(
            state,
            BeamBinaryHostNames.WriteContext,
            X(0),
            context
        );
        Call(
            state,
            BeamBinaryHostNames.Write,
            1,
            Term.I(37)
        );

        return state;
    }

    private static bool Match(object code, object state, Array operands)
    {
        Validate(
            code,
            BeamBinaryHostNames.MatchName,
            BeamBinaryHostNames.MatchOpcode,
            3,
            operands
        );

        return (bool)Call(
            Type(BeamBinaryHostNames.Execution),
            BeamBinaryHostNames.MatchMethod,
            code,
            state,
            operands
        );
    }

    internal static Task Values()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach (BigInteger expected in new[] { BigInteger.Zero, BigInteger.One << 127, (BigInteger.One << 128) - 1 })
        {
            byte[] bytes = new byte[16];
            byte[] value = expected.ToByteArray(isUnsigned: true, isBigEndian: true);
            value.CopyTo(bytes, bytes.Length - value.Length);
            object state = State(new(bytes), out object context);
            Require(Match(code, state, Commands()));
            Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == expected);
            Require((int)Property(context, BeamBinaryHostNames.Position) == 128);
            Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
        }

        return Task.CompletedTask;
    }

    internal static Task OffsetAndUnit()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        byte[] bytes = new byte[17];
        for (int bit = 0; bit < 128; bit++)
            bytes[(bit + 3) / 8] |= (byte)(1 << (7 - (bit + 3) % 8));
        object state = State(new(bytes, 131), out object context);
        SetPosition(context, 3);
        Require(Match(code, state, Commands(64, 2)));
        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == (BigInteger.One << 128) - 1);
        Require((int)Property(context, BeamBinaryHostNames.Position) == 131);

        return Task.CompletedTask;
    }

    internal static Task ShortAndBounds()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        object state = State(new(new byte[16], 127), out object context);
        Require(!Match(code, state, Commands()));
        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
        Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
        foreach (Array operands in new[] { Commands(129), Commands(65, 2), Commands(128, 0), Commands(1, 257), Commands(int.MaxValue, 256) })
            Refuses<NotSupportedException>(() => Validate(
                code,
                BeamBinaryHostNames.MatchName,
                BeamBinaryHostNames.MatchOpcode,
                3,
                operands
            ));
        Array flags = Commands();
        flags.SetValue(U(1), 8);
        Refuses<NotSupportedException>(() => Validate(
            code,
            BeamBinaryHostNames.MatchName,
            BeamBinaryHostNames.MatchOpcode,
            3,
            flags
        ));

        return Task.CompletedTask;
    }
}
