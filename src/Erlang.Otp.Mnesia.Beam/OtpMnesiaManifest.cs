namespace Erlang.Otp;

internal sealed record OtpMnesiaManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
