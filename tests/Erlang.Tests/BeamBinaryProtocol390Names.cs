namespace Erlang.Tests;

internal static class BeamBinaryProtocol390Names
{
    internal const string Prefix = "runtime/beam/binary390/";
    internal const string PositionRoundtrip = Prefix + "position-y-trim-roundtrip";
    internal const string OpaquePositionEscape = Prefix + "opaque-position-refuses-return-native-forgery";
    internal const string ResumeRejectsOrdinary = Prefix + "resume-callee-rejects-ordinary-argument";
    internal const string LocalContextTransport = Prefix + "local-context-resume-transport";
    internal const string UnitTwo = Prefix + "match-unit-two-success-and-failure";
    internal const string LiteralNoFail = Prefix + "nofail-literal-binary-source";
    internal const string ResumeAtomName = "resume";
    internal const string PositionFactory = "SavePosition";
    internal const string Failure = "BEAM binary390 protocol control failed";
    internal const string LiteralChunk = "LitT";
    internal const int ResumeAtom = 17;
    internal const int CodeFunctionCountOffset = 16;
    internal const int AtomCountBytes = 4;
    internal const int FormBytes = 12;
    internal const int ChunkHeaderBytes = 8;
    internal const int Alignment = 4;
    internal const int FormSizeOffset = 4;
    internal const int FormSizeBias = 8;
    internal const byte Move = 64;
    internal const byte Return = 19;
    internal const byte Label = 1;
    internal const byte CallOnly = 6;
    internal const byte ExternalTail = 78;
    internal const byte Allocate = 12;
    internal const byte InitializeY = 172;
    internal const byte Trim = 136;
    internal const byte Deallocate = 18;
    internal const byte ExtractBinary = 119;
    internal const byte ExtractTail = 165;
    internal const byte SavePosition = 167;
    internal const byte RestorePosition = 168;
    internal const byte ArgumentList = 0x17;
    internal const byte Literal = 0x47;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
