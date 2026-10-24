namespace Erlang.Otp;

internal sealed record OtpXmerlManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
