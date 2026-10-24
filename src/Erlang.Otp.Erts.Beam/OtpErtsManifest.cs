namespace Erlang.Otp;

internal sealed record OtpErtsManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
