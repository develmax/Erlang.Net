namespace Erlang.Otp;

internal sealed record OtpSslManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
