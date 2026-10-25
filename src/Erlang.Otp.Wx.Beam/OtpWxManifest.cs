namespace Erlang.Otp;

internal sealed record OtpWxManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
