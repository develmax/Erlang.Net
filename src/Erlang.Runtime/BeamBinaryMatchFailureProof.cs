namespace Erlang;

internal sealed class BeamBinaryMatchFailureProof
{
    private int lowerBound;
    private int modulus = 1;
    private int residue;
    private int? exact;

    internal bool MayFailAndAdvance(BeamBinaryCommand command)
    {
        switch (command.Kind)
        {
            case BeamBinaryCommandKind.EnsureAtLeast:
                lowerBound = Math.Max(lowerBound, command.Bits);
                modulus = command.Unit;
                residue = command.Bits % modulus;

                return true;

            case BeamBinaryCommandKind.EnsureExactly:
                lowerBound = command.Bits;
                exact = command.Bits;

                return true;

            case BeamBinaryCommandKind.Tail:
                return exact is { } bits
                    ? bits % command.Unit != 0
                    : modulus % command.Unit != 0 || residue % command.Unit != 0;

            case BeamBinaryCommandKind.Skip:
            case BeamBinaryCommandKind.Integer:
            case BeamBinaryCommandKind.Binary:
            case BeamBinaryCommandKind.Equal:
                bool mayFail = lowerBound < command.Bits || command.Kind == BeamBinaryCommandKind.Equal;
                lowerBound = Math.Max(0, lowerBound - command.Bits);
                residue = ((residue - command.Bits) % modulus + modulus) % modulus;
                if (exact is { } remaining)
                    exact = Math.Max(0, remaining - command.Bits);

                return mayFail;

            default:
                throw new InvalidDataException(BeamBinaryNames.InvalidContext);
        }
    }
}
