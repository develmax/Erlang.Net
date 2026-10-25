namespace Erlang.Otp;

internal sealed record OtpOdbcManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
