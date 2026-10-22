namespace Erlang;

internal static class ErlDriverPrimitiveNames422
{
    internal const string InvalidOwner = "Native driver primitive requires its actual live allocation or library owner";
    internal const string InvalidLayout = "Native driver primitive ABI does not match the measured Win64 layout";
    internal const string InvalidSystemInfo = "driver_system_info requires at least the original smp_support field extent";
    internal const string Kernel = "kernel32.dll";
    internal const string Extension = ".dll";
    internal const string Unspecified = "Unspecified error";
    internal const string TooLong = "Library name too long";
    internal const string ErrnoPrefix = "errno_";
    internal const int DynamicOffset = -10, NameTooLong = -4, MaximumPath = 4096, MaximumError = 255;
    internal const int DriverMajor = 3, DriverMinor = 3;
    internal const int Second = 0, Millisecond = 1, Microsecond = 2, Nanosecond = 3;
    internal const int Acp = 0, InsufficientBuffer = 122;
    internal const long TimeError = long.MinValue;
    internal const uint AlteredSearchPath = 8, FormatFromSystem = 0x1000;
}
