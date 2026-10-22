namespace Erlang.BeamBuild;

internal static class BeamBuildValidationNames
{
    internal const string ModuleIdentifier = @"\A[a-z][a-zA-Z0-9_]*\z";
    internal const string Sha256 = @"\A[0-9a-fA-F]{64}\z";
}
