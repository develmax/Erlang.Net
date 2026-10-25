namespace Erlang.Otp;

internal sealed record OtpDiameterManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
