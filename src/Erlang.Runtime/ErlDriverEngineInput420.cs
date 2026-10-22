namespace Erlang;

internal sealed record ErlDriverEngineInput420(
    byte[] LayoutJson,
    string LayoutSha256,
    IReadOnlyDictionary<int, string> NativeErrnoNames,
    string BridgePath,
    string BridgeSha256,
    ErlDriverSystemInfo422 SystemInfo
);
