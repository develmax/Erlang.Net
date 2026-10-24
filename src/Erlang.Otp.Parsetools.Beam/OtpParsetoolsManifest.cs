namespace Erlang.Otp;

internal sealed record OtpParsetoolsManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
