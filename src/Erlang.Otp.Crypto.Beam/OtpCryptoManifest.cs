namespace Erlang.Otp;

internal sealed record OtpCryptoManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
