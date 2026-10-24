namespace Erlang.Tests;

internal static class BeamBinaryErrorPriorityHostNames
{
    internal const string BadInteger = "bad_integer";
    internal const string BadBinary = "bad_binary";
    internal const string All = "all";
    internal const string Failure = "Expected original binary-construction failure reason";
    internal const string IntegerSizeFirst = "runtime/beam/binary382/error-priority/integer-size-first";
    internal const string BinarySizeFirst = "runtime/beam/binary382/error-priority/binary-size-first";
    internal const string ZeroWidthChecksType = "runtime/beam/binary382/error-priority/zero-width-checks-type";
    internal const string LaterSizeBeforeEarlierValue = "runtime/beam/binary382/error-priority/later-size-before-earlier-value";
    internal const string BinaryAllTypeStaysFirstPass = "runtime/beam/binary382/error-priority/binary-all-type-first-pass";
    internal const string BinaryAllUnitAfterLaterSize = "runtime/beam/binary382/error-priority/binary-all-unit-after-later-size";
}
