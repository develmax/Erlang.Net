namespace Erlang.Otp;

internal sealed record OtpMegacoManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
