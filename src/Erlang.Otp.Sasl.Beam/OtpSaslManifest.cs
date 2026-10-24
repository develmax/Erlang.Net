namespace Erlang.Otp;

internal sealed record OtpSaslManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
