namespace Erlang.Otp;

internal sealed record OtpObserverManifest(
    Dictionary<string, string> Images,
    Dictionary<string, string> Resources
);
