namespace Erlang.Otp;

internal sealed record OtpTftpManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
