namespace Erlang.Otp;

internal sealed record OtpRuntimeToolsManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
