using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPrettyEquality392Checks
{
    private static object Op(string kind, int value) => BeamBinaryHostAccess.Op(kind, value);

    private static object U(int value) => Op(BeamBinaryHostNames.Unsigned, value);

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static Array Commands(int width, BigInteger expected, int unit = 1)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            Op(BeamBinaryHostNames.Count, 7),
            Op(BeamBinaryHostNames.AtomKind, BeamBinaryHostNames.EnsureAtom),
            U(width),
            U(unit),
            Op(BeamBinaryHostNames.AtomKind, BeamBinaryHostNames.EqualAtom),
            Op(BeamBinaryHostNames.NilKind, 0),
            U(width),
            New(
                BeamBinaryHostNames.Operand,
                BeamBinaryHostNames.Unsigned,
                expected,
                null
            )
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

    internal static Task Width16()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        object state = State(new([0x3c, 0x3c, 0x58]), out object context);
        Require(Match(code, state, Commands(16, 15420, unit: 8)));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 16);
        object mismatch = State(new([0x3c, 0x3e]), out object failed);
        Require(!Match(code, mismatch, Commands(16, 15420)));
        Require((int)Property(failed, BeamBinaryHostNames.Position) == 16);
        object shortInput = State(new([0x3c]), out object shortContext);
        Require(!Match(code, shortInput, Commands(16, 15420)));
        Require((int)Property(shortContext, BeamBinaryHostNames.Position) == 0);

        return Task.CompletedTask;
    }

    internal static Task Unaligned16()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        byte[] source = new byte[4];
        byte[] payload = [0x3c, 0x3c, 0x58];
        for (int index = 0; index < 24; index++)
            source[(index + 3) / 8] |= (byte)(((payload[index / 8] >> (7 - index % 8)) & 1) << (7 - (index + 3) % 8));
        object state = State(new(source, 27), out object context);
        SetPosition(context, 3);
        Require(Match(code, state, Commands(16, 15420, unit: 8)));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 19);
        Require(((BitString)Property(context, BeamBinaryHostNames.Source)).SliceBits(19, 8).Equals(new BitString([0x58])));

        return Task.CompletedTask;
    }

    internal static Task ParserBounds()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach (Array operands in new[] { Commands(65, 0), Commands(16, 65536), Commands(16, -1) })
            Refuses<NotSupportedException>(() => Validate(
                code,
                BeamBinaryHostNames.MatchName,
                BeamBinaryHostNames.MatchOpcode,
                3,
                operands
            ));
        Array wrongKind = Commands(16, 15420);
        wrongKind.SetValue(Op(BeamBinaryHostNames.IntegerKind, 15420), 9);
        Refuses<NotSupportedException>(() => Validate(
            code,
            BeamBinaryHostNames.MatchName,
            BeamBinaryHostNames.MatchOpcode,
            3,
            wrongKind
        ));
        Array wrongFlags = Commands(16, 15420);
        wrongFlags.SetValue(Op(BeamBinaryHostNames.AtomKind, 7), 7);
        Refuses<NotSupportedException>(() => Validate(
            code,
            BeamBinaryHostNames.MatchName,
            BeamBinaryHostNames.MatchOpcode,
            3,
            wrongFlags
        ));

        return Task.CompletedTask;
    }

    internal static Task WideHost()
    {
        // This tests the private plan/runtime. It does not claim the existing
        // generic unsigned compact-operand decoder accepts every64bit value.
        object code = Code(BeamBinaryHostBytes.Image());
        object state = State(new([0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10]), out object context);
        Require(Match(code, state, Commands(64, 0xfedcba9876543210UL)));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 64);

        return Task.CompletedTask;
    }
}
