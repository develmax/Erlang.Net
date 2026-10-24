using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamUnicodeField392Checks
{
    private const string Fields = "BeamUnicodeFields";
    private const string Get = "Get";
    private const string Encode = "Encode";
    private const string Utf16Name = "bs_get_utf16";
    private const string Utf32Name = "bs_get_utf32";
    private const byte Utf16Opcode = 140;
    private const byte Utf32Opcode = 142;
    private const int Little = 2;
    private const int Native = 16;
    private const int ConflictingEndian = 18;

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static Array Operands(int flags = 0)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            Op(BeamBinaryHostNames.Unsigned, 2),
            Op(BeamBinaryHostNames.Unsigned, flags),
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

    internal static Task RoundTrip()
    {
        foreach (int scalar in new[] { 0, 0x41, 0xd7ff, 0xe000, 0xffff, 0x10000, 0x1f600, 0x10ffff })
            foreach (bool utf32 in new[] { false, true })
                foreach (bool little in new[] { false, true })
                {
                    BitString encoded = (BitString)Call(
                        Type(Fields),
                        Encode,
                        Term.I(scalar),
                        utf32,
                        little
                    );
                    object state = State(encoded, out object context);
                    Require((bool)Call(
                        Type(Fields),
                        Get,
                        state,
                        Operands(little ? Little : 0),
                        utf32
                    ));
                    Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == scalar);
                    Require((int)Property(context, BeamBinaryHostNames.Position) == encoded.BitLength);
                    Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
                }

        return Task.CompletedTask;
    }

    internal static Task IndependentBytesAndOffset()
    {
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            false,
            false
        )).Equals(new BitString([0xd8, 0x3d, 0xde, 0x00])));
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            false,
            true
        )).Equals(new BitString([0x3d, 0xd8, 0x00, 0xde])));
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            true,
            false
        )).Equals(new BitString([0x00, 0x01, 0xf6, 0x00])));
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            true,
            true
        )).Equals(new BitString([0x00, 0xf6, 0x01, 0x00])));
        byte[] source = new byte[5];
        byte[] value = [0xd8, 0x3d, 0xde, 0x00];
        for (int bit = 0; bit < 32; bit++)
            source[(bit + 3) / 8] |= (byte)(((value[bit / 8] >> (7 - bit % 8)) & 1) << (7 - (bit + 3) % 8));
        object state = State(new(source, 35), out object context);
        SetPosition(context, 3);
        Require((bool)Call(
            Type(Fields),
            Get,
            state,
            Operands(),
            false
        ));
        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 0x1f600);
        Require((int)Property(context, BeamBinaryHostNames.Position) == 35);

        return Task.CompletedTask;
    }

    internal static Task InvalidAndFlags()
    {
        foreach ((byte[] source, bool utf32) in new (byte[], bool)[]
        {
            ([], false), ([0xd8], false), ([0xdc, 0], false), ([0xd8, 0, 0, 0x41], false),
            ([0, 0, 0], true), ([0, 0, 0xd8, 0], true), ([0, 0x11, 0, 0], true), ([0xff, 0xff, 0xff, 0xff], true)
        })
        {
            object state = State(new(source), out object context);
            Require(!(bool)Call(
                Type(Fields),
                Get,
                state,
                Operands(),
                utf32
            ));
            Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
            Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
        }
        foreach (int invalid in new[] { -1, 0xd800, 0xdfff, 0x110000 })
            foreach (bool utf32 in new[] { false, true })
                Refuses<ErlangException>(() => Call(
                    Type(Fields),
                    Encode,
                    Term.I(invalid),
                    utf32,
                    false
                ));
        object code = Code(BeamBinaryHostBytes.Image());
        foreach (int flags in new[] { 0, Little, Native })
        {
            Validate(
                code,
                Utf16Name,
                Utf16Opcode,
                5,
                Operands(flags)
            );
            Validate(
                code,
                Utf32Name,
                Utf32Opcode,
                5,
                Operands(flags)
            );
        }
        foreach (int flags in new[] { 1, 3, 4, ConflictingEndian })
        {
            Refuses<NotSupportedException>(() => Validate(
                code,
                Utf16Name,
                Utf16Opcode,
                5,
                Operands(flags)
            ));
            Refuses<NotSupportedException>(() => Validate(
                code,
                Utf32Name,
                Utf32Opcode,
                5,
                Operands(flags)
            ));
        }

        return Task.CompletedTask;
    }
}
