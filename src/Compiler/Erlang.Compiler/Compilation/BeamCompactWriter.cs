// SPDX-License-Identifier: Apache-2.0
// Encoding based on OTP-29.1.1 lib/compiler/src/beam_asm.erl.
// Original Erlang implementation copyright Ericsson AB 1996-2026.

using System.Numerics;

namespace Erlang.Compiler;

internal static class BeamCompactWriter
{
    private const int MediumValueLimit = 1 << 11;

    internal static void Write(Stream output, int tag, BigInteger value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tag);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tag, BeamWire.TagMask);

        if (value.Sign >= 0)
        {
            if (value < (1 << BeamWire.CompactSmallPayloadShift))
            {
                output.WriteByte((byte)(((int)value << BeamWire.CompactSmallPayloadShift) | tag));

                return;
            }

            if (value < MediumValueLimit)
            {
                int word = (int)value;
                int high = (word >> BeamWire.CompactMediumWordShift) << BeamWire.CompactMediumHighShift;
                output.WriteByte((byte)(high | BeamWire.LongFlag | tag));
                output.WriteByte(unchecked((byte)word));

                return;
            }
        }

        byte[] payload = value.ToByteArray(isUnsigned: false, isBigEndian: true);
        int length = Math.Max(BeamWire.ShortLargeBase, payload.Length);
        int lengthCode = length - BeamWire.ShortLargeBase;
        int prefix = BeamWire.LongFlag | BeamWire.LargeFlag | tag;
        output.WriteByte((byte)(prefix | (Math.Min(lengthCode, BeamWire.ExtendedSizeCode) << BeamWire.CompactLargeSizeShift)));

        if (length >= BeamWire.ExtendedLargeBase)
        {
            Write(output, BeamWire.UnsignedTag, length - BeamWire.ExtendedLargeBase);
        }

        if (payload.Length < length)
        {
            output.WriteByte(byte.MaxValue);
        }

        output.Write(payload);
    }
}
