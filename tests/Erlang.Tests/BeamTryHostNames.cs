namespace Erlang.Tests;

internal static class BeamTryHostNames
{
    internal const string Prefix = "runtime/beam/try378/";
    internal const string Module = "beam_try_fixture378";
    internal const string Capture = "capture";
    internal const string Cases = "cases";
    internal const string Normal = "normal";
    internal const string GuestResource = "Erlang.Tests.BeamTry378.guest.beam";
    internal const string OriginalResource = "Erlang.Tests.BeamTry378.original.etf";
    internal const string GuestSha256 = "66F974334C97C3D363F8A5D11125D5B88A88084328B5F8E3037DEB540123A801";
    internal const string OriginalSha256 = "F903B83BDD5EC649CB6C2ADE6C9870DAA963C445FA917E9E54F913A3F30476BC";
    internal const string GuestSourceSha256 = "9A7FD7DCA5CB31461E643E98E2217F9F742E2EACE9631CB3218557463826BD80";
    internal const string OriginalObserverSha256 = "D7D5962ECB08E674E33DE29EC82E72854FD418C074A1A9395A3CD14D5452DDAA";
    internal const string OriginalObserverSourceSha256 = "315619F5BA1E8E5F33BF28BC479A7B9346941958AAF76126DD77DB15FDA5AE3F";
    internal const string OriginalCompilerSha256 = "8F1CD8036F83AC45C4A3173DF965F6503059EB99E0C14322A5D376EDC8CF7769";
    internal const string OriginalRuntimeSha256 = "D91699E88F417E00470D224934D47CD92358CC1B3B37CA1F15BCF5D4731D8F4E";
    internal const string SourceCommit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string ReleaseTag = "OTP-29.1.1";
    internal const string EvidenceFailure = "Embedded try guest or original whole-outcome evidence is invalid";
    internal const string OwnerFailure = "Try guest export is not owned by the exact prepared image";
    internal const string MissingReport = "Try guest child did not send its actual outcome to the owner mailbox";
    internal const string ExitFailure = "Try guest child or owner did not complete normally";
    internal const string OutcomeFailure = "Try guest complete outcome differs from original OTP";
    internal const int CaseCount = 17;
    internal static readonly TimeSpan CaseDeadline = TimeSpan.FromSeconds(5);
}
