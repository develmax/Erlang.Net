namespace Erlang.Tests;

internal static class NodeNative501Names
{
    internal const string TestPrefix = "runtime/node-native501/";
    internal const string Prefix = "Erlang.Tests.";
    internal const string Separator = ".";
    internal const string Failure = "Node or native pending contract differs from pinned OTP";
    internal const string Manifest = "manifest.json";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal static readonly (string Family, string Subject, int Values, int Profiles, bool Atomics, string ManifestHash)[] Groups =
    [
        ("NodeContract501", "node_contract501", 134, 670, true, "6E07D29A36389F934DD15BC0859CCA693D83E3ACD591A7D6784A3967D370183E"),
        ("NodeIdentity501", "node_identity501", 1, 5, true, "F9BA6137903612C93937C9407D8945ED2692F9D6C766D4F6FF4D02A92C783DCB"),
        ("NativeConnection501", "connection_capacity501", 1, 5, false, "B61C67F958DA83DC1175E855CAC15BEA5CA562BEF842E618FE217AC37753976F")
    ];
}
