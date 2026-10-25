namespace Erlang.BeamBuild;

internal static class BeamApplicationBuildNames
{
    internal const string CliMode = "--application-request";
    internal const string State = "beam-application-state.json";
    internal const string Lock = ".application-build.lock";
    internal const string StagePrefix = ".application-stage-";
    internal const string StateSchema = "application-resources-state-v1";
    internal const string PublicSchema = "application-resources-v1";
    internal const string NativeRequest = "application_resource_request";
    internal const string RequestFile = "request.etf";
    internal const string ResultFile = "parsed.etf";
    internal const string OwnedInputs = "inputs";
    internal const string StagedManifest = "application-resources.json";
    internal const string BeamKind = "beam";
    internal const string DescriptorKind = "descriptor";
    internal const string PrivKind = "priv";
    internal const string ConfigurationKind = "config";
    internal const string ManifestKind = "manifest";
}
