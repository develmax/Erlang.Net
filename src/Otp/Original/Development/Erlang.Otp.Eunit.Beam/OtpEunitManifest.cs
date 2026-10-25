namespace Erlang.Otp;

internal sealed record OtpEunitManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
