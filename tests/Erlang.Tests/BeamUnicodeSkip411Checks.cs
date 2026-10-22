using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamUnicodeSkip411Names;

namespace Erlang.Tests;

internal static class BeamUnicodeSkip411Checks
{
    private static Array Operands(int flags = 0) => Ops(
        Op(BeamBinaryHostNames.Label, 3),
        Op(BeamBinaryHostNames.X, 0),
        Op(BeamBinaryHostNames.Unsigned, 2),
        Op(BeamBinaryHostNames.Unsigned, flags)
    );

    private static object State(BitString source, int position, out object context)
    {
        object state = New(BeamBinaryHostNames.RegisterState);
        context = New(BeamBinaryHostNames.MatchContext, source);
        SetPosition(context, position);
        Call(
            state,
            BeamBinaryHostNames.WriteContext,
            Op(BeamBinaryHostNames.X, 0),
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

    internal static Task Success()
    {
        foreach (bool utf32 in new[] { false, true })
            foreach (bool little in new[] { false, true })
                foreach (int scalar in new[] { 0, 65, 0xd7ff, 0xe000, 0xffff, 0x10000, 0x1f600, 0x10ffff })
                    foreach (int offset in new[] { 0, 3 })
                    {
                        BitString encoded = (BitString)Call(
                            Type(Fields),
                            Encode,
                            Term.I(scalar),
                            utf32,
                            little
                        );
                        byte[] bytes = new byte[(offset + encoded.BitLength + 7) / 8];
                        byte[] raw = encoded.ToArray();
                        for (int bit = 0; bit < encoded.BitLength; bit++)
                            bytes[(bit + offset) / 8] |= (byte)(((raw[bit / 8] >> (7 - bit % 8)) & 1) << (7 - (bit + offset) % 8));
                        object state = State(new BitString(bytes, encoded.BitLength + offset), offset, out object context);
                        Require((bool)Call(
                            Type(Fields),
                            Skip,
                            state,
                            Operands(little ? 2 : 0),
                            utf32
                        ));
                        Require((int)Property(context, BeamBinaryHostNames.Position) == offset + encoded.BitLength);
                        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
                        Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
                    }

        return Task.CompletedTask;
    }

    internal static Task Failure()
    {
        foreach ((byte[] bytes, bool utf32) in new (byte[], bool)[]
        {
            ([], false), ([216], false), ([220, 0], false), ([216, 0], false),
            ([216, 0, 0, 65], false), ([0, 0, 0], true),
            ([0, 0, 216, 0], true), ([0, 17, 0, 0], true), ([255, 255, 255, 255], true)
        })
        {
            object state = State(new BitString(bytes), 0, out object context);
            Require(!(bool)Call(
                Type(Fields),
                Skip,
                state,
                Operands(),
                utf32
            ));
            Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
            Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
            Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
        }

        return Task.CompletedTask;
    }

    internal static Task Admission()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach ((string name, byte opcode) in new[] { (Utf16, Opcode16), (Utf32, Opcode32) })
        {
            foreach (int flags in new[] { 0, 2, 16 })
                Validate(
                    code,
                    name,
                    opcode,
                    4,
                    Operands(flags)
                );
            foreach (int flags in new[] { 1, 3, 4, 18 })
                Refuses<NotSupportedException>(() => Validate(
                    code,
                    name,
                    opcode,
                    4,
                    Operands(flags)
                ));
            Array extra = Ops(
                Op(BeamBinaryHostNames.Label, 3),
                Op(BeamBinaryHostNames.X, 0),
                Op(BeamBinaryHostNames.Unsigned, 2),
                Op(BeamBinaryHostNames.Unsigned, 0),
                Op(BeamBinaryHostNames.X, 1)
            );
            Refuses<NotSupportedException>(() => Validate(
                code,
                name,
                opcode,
                4,
                extra
            ));
        }

        return Task.CompletedTask;
    }
}
