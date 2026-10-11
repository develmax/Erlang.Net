namespace Erlang;

internal static class ErlangHashParameters
{
    public const uint BaseMix = 0x9e3779b9;
    public const uint NilMix = 0x3c6ef372;
    public const uint AtomMix = 0xdaa66d2b;
    public const uint StringMix = 0x78dde6e4;
    public const uint PidMix = 0x1715609d;
    public const uint PortMix = 0xb54cda56;
    public const uint ReferenceMix = 0x5384540f;
    public const uint TupleMix = 0x8ff34781;
    public const uint NegativeIntegerMix = 0x2e2ac13a;
    public const uint PositiveIntegerMix = 0xcc623af3;
    public const uint FloatMix = 0x6a99b4ac;
    public const uint BinaryMix = 0x08d12e65;
    public const uint ExternalFunctionMix = 0xa708a81e;
    public const uint BitTailMix = 0x454021d7;
    public const uint MapSizeMix = 0xe3779b90;
    public const uint MapPairsMix = 0xbe1e08bb;
    public const uint EmptyListHash = 3468870702;
    public const int NilTag = 2;
    public const int IntegerSmallBits = 28;
    public const int IntegerWordBits = 32;
    public const int IntegerWordBytes = 4;
    public const int IntegerDigitBytes = 8;
    public const int ByteBits = 8;
    public const int StringGroupBytes = 4;
    public const int AtomNibbleBits = 4;
    public const uint AtomHighMask = 0xf0000000;
    public const int AtomFoldBits = 24;
    public const int Latin1ContinuationBits = 6;
    public const int Latin1LeadMask = 0xfe;
    public const int Latin1Lead = 0xc2;
    public const int ContinuationMask = 0xc0;
    public const int Continuation = 0x80;
    public const int ContinuationPayloadMask = 0x3f;
}
