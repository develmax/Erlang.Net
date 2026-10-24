using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFloatString392Checks
{
    private const string Execution = "BeamFloatStringExecution";
    private const string FloatMethod = "Float";
    private const string StringMethod = "String";
    private const string FloatName = "bs_get_float2";
    private const string StringName = "bs_match_string";
    private const byte FloatOpcode = 118;
    private const byte StringOpcode = 132;

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static object U(int value) => Op(BeamBinaryHostNames.Unsigned, value);

    private static Array FloatOperands(int bits, int flags = 0, int unit = 1)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            U(2),
            Op(BeamBinaryHostNames.IntegerKind, bits),
            U(unit),
            U(flags),
            X(1)
        );

    private static Array StringOperands(int bits, int offset = 0)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            U(bits),
            U(offset)
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

    internal static Task FloatWidthsEndianAndZero()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach ((byte[] payload, int bits) in new (byte[], int)[]
        {
            ([0x3e, 0], 16), ([0x3f, 0xc0, 0, 0], 32), ([0x3f, 0xf8, 0, 0, 0, 0, 0, 0], 64)
        })
            foreach (int flags in new[] { 0, 2, 16 })
            {
                bool little = flags == 2 || flags == 16 && BitConverter.IsLittleEndian;
                byte[] input = little ? payload.Reverse().ToArray() : payload;
                object state = State(new(input), out object context);
                Validate(
                    code,
                    FloatName,
                    FloatOpcode,
                    7,
                    FloatOperands(bits, flags)
                );
                Require((bool)Call(
                    Type(Execution),
                    FloatMethod,
                    code,
                    state,
                    FloatOperands(bits, flags)
                ));
                Require(((FloatTerm)Call(state, BeamBinaryHostNames.Read, 1)).Value == 1.5);
                Require((int)Property(context, BeamBinaryHostNames.Position) == bits);
                Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
            }
        object empty = State(new([]), out object emptyContext);
        Require((bool)Call(
            Type(Execution),
            FloatMethod,
            code,
            empty,
            FloatOperands(0, unit: 256)
        ));
        Require(BitConverter.DoubleToUInt64Bits(((FloatTerm)Call(empty, BeamBinaryHostNames.Read, 1)).Value) == 0);
        Require((int)Property(emptyContext, BeamBinaryHostNames.Position) == 0);
        object negativeZero = State(new([0x80, 0, 0, 0, 0, 0, 0, 0]), out _);
        Require((bool)Call(
            Type(Execution),
            FloatMethod,
            code,
            negativeZero,
            FloatOperands(64)
        ));
        Require(BitConverter.DoubleToUInt64Bits(((FloatTerm)Call(negativeZero, BeamBinaryHostNames.Read, 1)).Value) == 0x8000000000000000UL);

        return Task.CompletedTask;
    }

    internal static Task FloatFailuresAndFlags()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach ((byte[] input, int bits) in new (byte[], int)[]
        {
            ([0x7c, 0], 16), ([0x7e, 0], 16), ([0x7f, 0x80, 0, 0], 32), ([0x7f, 0xc0, 0, 1], 32),
            ([0x7f, 0xf0, 0, 0, 0, 0, 0, 0], 64), ([0x7f, 0xf8, 0, 0, 0, 0, 0, 1], 64),
            ([0], 32), ([0, 0, 0], 24), ([0], -1)
        })
        {
            object state = State(new(input), out object context);
            Require(!(bool)Call(
                Type(Execution),
                FloatMethod,
                code,
                state,
                FloatOperands(bits)
            ));
            Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
            Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
        }
        foreach (Array operands in new[] { FloatOperands(32, 1), FloatOperands(32, 3), FloatOperands(32, 18), FloatOperands(32, unit: 0), FloatOperands(32, unit: 257) })
            Refuses<NotSupportedException>(() => Validate(
                code,
                FloatName,
                FloatOpcode,
                7,
                operands
            ));

        return Task.CompletedTask;
    }

    internal static Task StringOffsetBoundsAndFailure()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        object state = State(new([10, 20, 30]), out object context);
        Validate(
            code,
            StringName,
            StringOpcode,
            4,
            StringOperands(16)
        );
        Require((bool)Call(
            Type(Execution),
            StringMethod,
            code,
            state,
            StringOperands(16)
        ));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 16);
        foreach (byte[] input in new[] { new byte[] { 10, 21 }, new byte[] { 10 } })
        {
            object invalid = State(new(input), out object invalidContext);
            Require(!(bool)Call(
                Type(Execution),
                StringMethod,
                code,
                invalid,
                StringOperands(16)
            ));
            Require((int)Property(invalidContext, BeamBinaryHostNames.Position) == 0);
        }
        byte[] shifted = new byte[3];
        byte[] pattern = [20, 30];
        for (int bit = 0; bit < 13; bit++)
            shifted[(bit + 3) / 8] |= (byte)(((pattern[bit / 8] >> (7 - bit % 8)) & 1) << (7 - (bit + 3) % 8));
        object unaligned = State(new(shifted, 17), out object offsetContext);
        SetPosition(offsetContext, 3);
        Require((bool)Call(
            Type(Execution),
            StringMethod,
            code,
            unaligned,
            StringOperands(13, 1)
        ));
        Require((int)Property(offsetContext, BeamBinaryHostNames.Position) == 16);
        Validate(
            code,
            StringName,
            StringOpcode,
            4,
            StringOperands(0, 4)
        );
        foreach (Array operands in new[] { StringOperands(1, 4), StringOperands(0, 5), StringOperands(int.MaxValue, int.MaxValue) })
            Refuses<NotSupportedException>(() => Validate(
                code,
                StringName,
                StringOpcode,
                4,
                operands
            ));

        return Task.CompletedTask;
    }
}
