namespace BeamFixtureExport;

internal static class FixtureExecutionNames
{
    public const string Option = "--execute";
    public const string Usage = "Usage: BeamFixtureExport <installed-otp-root> <new-output-directory> <source-build-evidence.json> [--execute]";
    public const string CompilationOnly = "Execution was not requested";
    public const string OnLoadNotObserved = "Compilation only: on_load callback process side effect requires a separate observer contract";
}
