namespace Erlang.Otp;

internal sealed record OtpToolsManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
