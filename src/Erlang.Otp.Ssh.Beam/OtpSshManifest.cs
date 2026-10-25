namespace Erlang.Otp;

internal sealed record OtpSshManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
