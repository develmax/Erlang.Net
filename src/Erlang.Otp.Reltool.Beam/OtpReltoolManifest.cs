namespace Erlang.Otp;

internal sealed record OtpReltoolManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
