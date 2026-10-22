using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinaryFlags395Checks
{
    private const string Wire = "BeamBinaryWireValues";
    private const string Numeric = "NumericFlags";
    private const string IntegerMethod = "Integer";
    private const string SourceBit = "IntegerSourceBit";
    private const string FloatMethod = "Float";

    internal static Task IntegerEndianSignAndOffset()
    {
        object little = New(BeamBinaryHostNames.MatchContext, new BitString([0xbc, 0xd0], 13));
        Require((BigInteger)Call(
            Type(Wire),
            IntegerMethod,
            little,
            13,
            2
        ) == 0x1abc);
        Require((BigInteger)Call(
            Type(Wire),
            IntegerMethod,
            little,
            13,
            6
        ) == -1348);
        object big = New(BeamBinaryHostNames.MatchContext, new BitString([0xd5, 0xe0], 13));
        Require((BigInteger)Call(
            Type(Wire),
            IntegerMethod,
            big,
            13,
            4
        ) == -1348);
        Require((BigInteger)Call(
            Type(Wire),
            IntegerMethod,
            little,
            0,
            6
        ) == 0);
        byte[] shifted = new byte[2];
        byte[] payload = [0xbc, 0xd0];
        for (int bit = 0; bit < 13; bit++)
            shifted[(bit + 3) / 8] |= (byte)(((payload[bit / 8] >> (7 - bit % 8)) & 1) << (7 - (bit + 3) % 8));
        object offset = New(BeamBinaryHostNames.MatchContext, new BitString(shifted));
        SetPosition(offset, 3);
        Require((BigInteger)Call(
            Type(Wire),
            IntegerMethod,
            offset,
            13,
            6
        ) == -1348);
        Require((int)Property(offset, BeamBinaryHostNames.Position) == 3);
        byte[] encoded = new byte[2];
        BigInteger value = 0x1abc;
        for (int bit = 0; bit < 13; bit++)
        {
            int source = (int)Call(
                Type(Wire),
                SourceBit,
                bit,
                13,
                true
            );
            encoded[bit / 8] |= (byte)((int)((value >> source) & 1) << (7 - bit % 8));
        }
        Require(new BitString(encoded, 13).Equals(new BitString([0xbc, 0xd0], 13)));

        return Task.CompletedTask;
    }

    internal static Task NativeFlagsAndRefusals()
    {
        foreach (int flags in new[] { 0, 2, 4, 6 })
            Require((int)Call(Type(Wire), Numeric, flags) == flags);
        Require((int)Call(Type(Wire), Numeric, 16) == (BitConverter.IsLittleEndian ? 2 : 0));
        Require((int)Call(Type(Wire), Numeric, 20) == (BitConverter.IsLittleEndian ? 6 : 4));
        foreach (int flags in new[] { -1, 1, 8, 18, 22, int.MaxValue })
            Refuses<NotSupportedException>(() => Call(Type(Wire), Numeric, flags));
        object code = Code(BeamBinaryHostBytes.Image());
        Array operands = Ops(
            Op(BeamBinaryHostNames.Label, 3),
            Op(BeamBinaryHostNames.X, 0),
            Op(BeamBinaryHostNames.Unsigned, 2),
            Op(BeamBinaryHostNames.Unsigned, 16),
            Op(BeamBinaryHostNames.X, 1)
        );
        Validate(
            code,
            "bs_get_utf16",
            140,
            5,
            operands
        );
        Validate(
            code,
            "bs_get_utf32",
            142,
            5,
            operands
        );

        return Task.CompletedTask;
    }

    internal static Task DynamicFloatBytesAndHalfRounding()
    {
        Require(((BitString)Call(
            Type(Wire),
            FloatMethod,
            new FloatTerm(1.5),
            16,
            false
        )).Equals(new BitString([0x3e, 0])));
        Require(((BitString)Call(
            Type(Wire),
            FloatMethod,
            new FloatTerm(1.5),
            32,
            true
        )).Equals(new BitString([0, 0, 0xc0, 0x3f])));
        Require(((BitString)Call(
            Type(Wire),
            FloatMethod,
            new FloatTerm(1.00048828125),
            16,
            false
        )).Equals(new BitString([0x3c, 0])));
        Require(((BitString)Call(
            Type(Wire),
            FloatMethod,
            new FloatTerm(1.0004883110523224),
            16,
            false
        )).Equals(new BitString([0x3c, 1])));
        Require(((BitString)Call(
            Type(Wire),
            FloatMethod,
            new FloatTerm(1.0e300),
            16,
            false
        )).Equals(new BitString([0x7c, 0])));
        Require(((BitString)Call(
            Type(Wire),
            FloatMethod,
            new FloatTerm(1.0e300),
            32,
            false
        )).Equals(new BitString([0x7f, 0x80, 0, 0])));
        foreach (int bits in new[] { 0, 24, 128 })
            Refuses<ErlangException>(() => Call(
                Type(Wire),
                FloatMethod,
                new FloatTerm(1.5),
                bits,
                false
            ));
        Refuses<ErlangException>(() => Call(
            Type(Wire),
            FloatMethod,
            new Integer(BigInteger.One << 1024),
            64,
            false
        ));

        return Task.CompletedTask;
    }
}
