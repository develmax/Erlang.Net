namespace Erlang.Otp;

internal sealed record OtpInetsManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
