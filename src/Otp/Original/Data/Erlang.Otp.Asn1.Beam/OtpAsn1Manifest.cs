namespace Erlang.Otp;

internal sealed record OtpAsn1Manifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
