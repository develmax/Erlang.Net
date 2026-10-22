namespace HelloBeamSource;

internal static class BeamSourceExampleNames
{
    internal const string Constant = "beam_fixture_constant";
    internal const string Add = "beam_fixture_add";
    internal const string Driver = "beam_project_driver";
    internal const string Host = "beam_project_host";
    internal const string Sum = "sum";
    internal const string Run = "run";
    internal const string Modules = "Modules";
    internal const string Manifest = "beam-build-state.json";
    internal const string BeamExtension = ".beam";
    internal const string Ok = "ok";
    internal const string Alpha = "alpha";
    internal const string Beta = "beta";
    internal const string Normal = "normal";
    internal const string Invalid = "Generated BEAM project manifest or module identity is invalid";
    internal const string Result = "Fresh BEAM project or C# host result differs";
    internal const string Success = "Fresh BEAM project: three modules, cross-module calls, C# collection and host MFA passed";
    internal const int FixtureValue = 42;
    internal const int ExpectedSum = 6;
    internal const int MutationSentinel = 99;
    internal static readonly string[] ExpectedModules = [Constant, Add, Driver];
}
