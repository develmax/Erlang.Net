namespace Erlang.Otp;

internal sealed record OtpDebuggerManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
