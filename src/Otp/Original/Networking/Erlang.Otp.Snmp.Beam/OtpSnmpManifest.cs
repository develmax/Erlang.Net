namespace Erlang.Otp;

internal sealed record OtpSnmpManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
