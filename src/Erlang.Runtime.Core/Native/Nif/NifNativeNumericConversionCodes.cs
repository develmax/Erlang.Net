namespace Erlang;

internal static class NifNativeNumericConversionCodes
{
    // Pinned OTP error.h: EXC_OFFSET=10, EXC_ERROR=EXF_SAVETRACE|EXF_LOG=80.
    internal const ulong BadArgument = (3UL << 10) | 80UL;
    internal const ulong SystemLimit = (15UL << 10) | 80UL;
}
