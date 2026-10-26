namespace Erlang.Otp;

internal sealed record OtpFtpManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
