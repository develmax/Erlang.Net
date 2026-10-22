namespace Erlang.BeamBuild;

internal static class BeamBuildNames
{
    internal const string Commit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string Tag = "OTP-29.1.1";
    internal const string Version = "29.1.1";
    internal const string Release = "29";
    internal const string Usage = "Usage: Erlang.BeamBuild <absolute-request.json>";
    internal const string Invalid = "Invalid or unsupported BEAM source build request";
    internal const string Compilation = "Original Erlang source compilation or compiler cleanup failed";
    internal const string Admission = "Original compiler admission failed";
    internal const string Manifest = "beam-build-state.json";
    internal const string Lock = ".beam-build.lock";
    internal const string BeamExtension = ".beam";
    internal const string SourceExtension = ".erl";
    internal const string EmulatorFlags = "ERL_FLAGS";
    internal const string EmulatorFlagsValue = "+S 1:1 +A 1";
    internal const string CompilerServer = "ERLC_USE_SERVER";
    internal const string CompilerServerDisabled = "false";
    internal static readonly string[] RemovedEnvironment = [EmulatorFlags, BeamCompilerEnvironmentNames.AdditionalFlags, BeamCompilerEnvironmentNames.TrailingFlags, BeamCompilerEnvironmentNames.Libraries, BeamCompilerEnvironmentNames.CompilerOptions, BeamCompilerEnvironmentNames.Emulator, CompilerServer, BeamCompilerEnvironmentNames.ServerIdentity, BeamCompilerEnvironmentNames.Configuration];
}
