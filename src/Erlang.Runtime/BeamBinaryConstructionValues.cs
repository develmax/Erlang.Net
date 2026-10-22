using System.Buffers.Binary;
using System.Text;

namespace Erlang;

internal static class BeamBinaryConstructionValues
{
    internal static ErlangException CaptureFailure(
        ProcessContext? context,
        ErlangException error,
        int segment,
        string kind,
        Term value,
        string cause = BeamBinaryNames.TypeFailure
    )
    {
        // Pure HOST construction controls have no guest source frame.
        if (context is null)
            return error;
        var information = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(BeamBinaryNames.FailureModule)] = Term.A(BeamBinaryNames.FailureFormatter),
            [Term.A(BeamBinaryNames.FailureFunction)] = Term.A(BeamBinaryNames.FormatFailure),
            [Term.A(BeamBinaryNames.FailureCause)] = Term.Tuple(
                Term.I(segment),
                Term.A(kind),
                Term.A(cause),
                value
            )
        });

        return error.WithCapturedStack(context.CaptureErrorStack(Term.A(BeamBinaryNames.NoArgumentOverride), information));
    }

    internal static BitString Utf8(Term value)
    {
        if (value is not Integer integer || integer.Value < 0
            || integer.Value > BeamBinaryFieldNames.UnicodeScalarMaximum
            || !Rune.TryCreate((int)integer.Value, out Rune scalar))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] bytes = new byte[scalar.Utf8SequenceLength];
        scalar.EncodeToUtf8(bytes);

        return new BitString(bytes);
    }

    internal static BitString Float64(Term value)
    {
        double floating = value switch
        {
            FloatTerm number => number.Value,
            Integer integer => (double)integer.Value,
            _ => double.NaN
        };
        if (!double.IsFinite(floating))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] bytes = new byte[sizeof(double)];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, BitConverter.DoubleToUInt64Bits(floating));

        return new BitString(bytes);
    }
}
