namespace Erlang.BeamBuild;

internal sealed record BeamApplicationState(
    string Schema,
    string ApplicationName,
    string Version,
    string RequestSha256,
    string ConfigurationSha256,
    BeamApplicationOutput[] Files,
    BeamApplicationNativeEvidence NativeEvidence
);
