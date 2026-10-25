namespace Erlang.Otp;

internal sealed record OtpEldapManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
