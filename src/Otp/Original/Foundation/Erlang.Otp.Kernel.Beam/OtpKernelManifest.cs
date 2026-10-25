namespace Erlang.Otp;

internal sealed record OtpKernelManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
