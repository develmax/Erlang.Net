using System.Numerics;

namespace Erlang;

internal static class BeamIntegerNormalization
{
    public static bool IsNormalized(BeamCompactValue value)
    {
        if (value.FitsNativeWord)
        {
            return true;
        }
        int magnitudeBytes = BigInteger.Abs(value.Value).GetByteCount(isUnsigned: true);

        return value.EncodedPayloadBytes - magnitudeBytes <= 1;
    }
}
