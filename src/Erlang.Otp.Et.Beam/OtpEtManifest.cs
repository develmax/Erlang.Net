namespace Erlang.Otp;

internal sealed record OtpEtManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
