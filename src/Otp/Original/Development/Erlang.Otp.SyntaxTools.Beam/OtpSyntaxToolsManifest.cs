namespace Erlang.Otp;

internal sealed record OtpSyntaxToolsManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
