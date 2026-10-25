namespace Erlang.Otp;

internal sealed record OtpCommonTestManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
