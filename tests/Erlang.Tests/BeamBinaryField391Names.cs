namespace Erlang.Tests;

internal static class BeamBinaryField391Names
{
    internal const string Prefix = "runtime/beam/binary391/";
    internal const string Utf8Failures = Prefix + "invalid-utf8-and-eof-consume-nothing";
    internal const string Utf8Unaligned = Prefix + "unaligned-utf8-get-and-skip";
    internal const string IntegerWide = Prefix + "unsigned-integer-wide-and-zero";
    internal const string SkipBounds = Prefix + "skip-zero-negative-overflow-truncation";
    internal const string RoleRefusals = Prefix + "units-flags-and-shapes-refuse";
    internal const string OpaqueContext = Prefix + "new-field-opcodes-preserve-opaque-boundary";
    internal const string Execution = "BeamBinaryFieldExecution";
    internal const string GetInteger = "GetInteger";
    internal const string Utf8 = "Utf8";
    internal const string Skip = "Skip";
    internal const string GetIntegerOpcodeName = "bs_get_integer2";
    internal const string GetUtf8OpcodeName = "bs_get_utf8";
    internal const string SkipUtf8OpcodeName = "bs_skip_utf8";
    internal const string SkipBitsOpcodeName = "bs_skip_bits2";
    internal const string Failure = "BEAM binary391 field protocol control failed";
    internal const byte GetIntegerOpcode = 117;
    internal const byte SkipBitsOpcode = 120;
    internal const byte GetUtf8Opcode = 138;
    internal const byte SkipUtf8Opcode = 139;
    internal const byte GetTailOpcode = 165;
    internal const byte ReturnOpcode = 19;
    internal const byte LabelOpcode = 1;
    internal const byte MoveOpcode = 64;
    internal const int BitOffset = 3;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
