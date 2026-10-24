namespace Erlang.Otp;

internal sealed record OtpEdocManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
