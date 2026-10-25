namespace Erlang.Otp;

internal sealed record OtpOsMonManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
