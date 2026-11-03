namespace Erlang.Tests;

internal static class BeamOrdinaryBinaryReceive525Names
{
    internal const string Test = "runtime/beam/receive525/original-driver-binary-case-before-receive";
    internal const string Resource = "dirty_flags521_driver525.beam";
    internal const string GuestModule = "dirty_flags521_driver";
    internal const string ExpectedSha256 = "26A4A41480CD482EA7A46C745A5A4DC9D40C7BFFAEB6552CF54A966AFBBFE1C0";
    internal const string ResourceSeparator = ".";
    internal const string Failure = "Whole genuine original receive525 BEAM admission failed.";
    internal static readonly string[] Dependencies = ["erlang", "lists", "file", "code", "dirty_flags521", GuestModule];
}
