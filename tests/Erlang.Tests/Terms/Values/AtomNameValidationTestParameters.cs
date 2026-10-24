namespace Erlang.Tests;

internal static class AtomNameValidationTestParameters
{
    internal const int MaximumScalars = 255;
    internal const int OverMaximumScalars = 256;
    internal const int SupplementaryScalar = 65536;
    internal const int SurrogateScalar = 55296;
    internal const int ConcurrentRejections = 128;
}
