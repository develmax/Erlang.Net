namespace HelloBeamIncludes;

internal static class BeamIncludesExampleNames
{
    internal const string Module = "hello_include";
    internal const string HelloFunction = "hello";
    internal const string ModulesDirectory = "Modules";
    internal const string ManifestFile = "beam-build-state.json";
    internal const string BeamExtension = ".beam";
    internal const string GreetingAtom = "hello";
    internal const string PayloadAtom = "included";
    internal const string FlavorAtom = "test";
    internal const string NormalExit = "normal";
    internal const string InvalidAssets = "Included BEAM manifest, digest or image identity is invalid";
    internal const string InvalidOwner = "Included module is not owned by its actual prepared BEAM image";
    internal const string InvalidResult = "Included macro project returned an unexpected value or actor exit";
    internal const string IncompleteActor = "Included macro actor completed without reporting its MFA result";
    internal const string Success = "Fresh included BEAM project: one module, two headers and typed macro result passed";
    internal const int IncludedValue = 377;
}
