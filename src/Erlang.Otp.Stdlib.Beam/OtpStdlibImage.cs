namespace Erlang.Otp;

internal sealed record OtpStdlibImage(
    string Name,
    string Sha256,
    string[] Dependencies,
    bool RequiresExplicitDependencies
);
