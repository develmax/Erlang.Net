namespace Erlang.Otp;

internal sealed record OtpDialyzerManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
