namespace Erlang.Otp;

internal sealed record OtpPublicKeyManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
