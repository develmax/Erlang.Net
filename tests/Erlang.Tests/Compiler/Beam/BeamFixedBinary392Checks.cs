using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFixedBinary392Checks
{
    private static object U(int value) => Op(BeamBinaryHostNames.Unsigned, value);

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static Array Commands(int size, int unit = 8, int destination = 1)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            Op(BeamBinaryHostNames.Count, 6),
            Op(BeamBinaryHostNames.AtomKind, BeamBinaryHostNames.BinaryAtom),
            U(2),
            Op(BeamBinaryHostNames.NilKind, 0),
            U(size),
            U(unit),
            X(destination)
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

    internal static Task LargeAndUnaligned()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        byte[] input = Enumerable.Range(0, 8193).Select(index => (byte)index).ToArray();
        object state = State(new(input), out object context);
        Require(Match(code, state, Commands(8192)));
        Require(((BitString)Call(state, BeamBinaryHostNames.Read, 1)).Equals(new BitString(input.AsSpan(0, 8192))));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 65536);
        Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
        object unaligned = State(new([0xab, 0xcd, 0xef]), out object offset);
        SetPosition(offset, 3);
        Require(Match(code, unaligned, Commands(2)));
        Require(((BitString)Call(unaligned, BeamBinaryHostNames.Read, 1)).Equals(new BitString([0x5e, 0x6f])));
        Require((int)Property(offset, BeamBinaryHostNames.Position) == 19);

        return Task.CompletedTask;
    }

    internal static Task EmptyBitstringAndAlias()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        object state = State(new([0xab]), out object context);
        Require(Match(code, state, Commands(0)));
        Require(((BitString)Call(state, BeamBinaryHostNames.Read, 1)).BitLength == 0);
        Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
        Require(Match(code, state, Commands(3, unit: 1)));
        Require(((BitString)Call(state, BeamBinaryHostNames.Read, 1)).Equals(new BitString([0xa0], 3)));
        object alias = State(new([0x81, 0x82]), out object aliasContext);
        Require(Match(code, alias, Commands(1, destination: 0)));
        Require(((BitString)Call(alias, BeamBinaryHostNames.Read, 0)).Equals(new BitString([0x81])));
        Require((int)Property(aliasContext, BeamBinaryHostNames.Position) == 8);

        return Task.CompletedTask;
    }

    internal static Task ShortFlagsAndOverflow()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        object state = State(new([0xab]), out object context);
        Require(!Match(code, state, Commands(2)));
        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
        Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
        foreach (Array operands in new[] { Commands(int.MaxValue), Commands(1, 0), Commands(1, 257) })
            Refuses<NotSupportedException>(() => Validate(
                code,
                BeamBinaryHostNames.MatchName,
                BeamBinaryHostNames.MatchOpcode,
                3,
                operands
            ));
        Array flags = Commands(1);
        flags.SetValue(U(1), 5);
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
