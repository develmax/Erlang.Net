namespace Erlang.Otp;

internal sealed record OtpCompilerManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
