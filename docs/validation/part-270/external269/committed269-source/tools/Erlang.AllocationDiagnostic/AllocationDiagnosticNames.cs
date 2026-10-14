namespace AllocationDiagnostic;

internal static class AllocationDiagnosticNames
{
    internal const int ArgumentCount = 1;
    internal const string InvalidWindows = "DiagnosticInvalidMeasurementWindows";
    internal const string Usage = "Usage: Erlang.AllocationDiagnostic newOutput.json";
    internal const string FreshOutput = "Output report must be new";
    internal const string RowSummary = " rows=";
    internal const string Module = "erlang";
    internal const string Function = "phash2";
    internal const string AlgorithmType = "Erlang.ErlangNativeHash";
    internal const string AlgorithmMethod = "Portable2";
    internal const string RegisteredFunctions = "functions";
    internal const string ExactWindow = "ExactOriginalFirstWindow";
    internal const string RegistryWindow = "OriginalRegistryWindow";
    internal const string PrebuiltWindow = "PrebuiltArgumentsRegistry";
    internal const string BodyWindow = "RegisteredBody";
    internal const string DirectWindow = "Portable2Delegate";
    internal const string ArgumentsWindow = "ArgumentConstructionOnly";
    internal const string Completed = "DiagnosticCompleted";
    internal const string Failed = "DiagnosticFailed";
    internal const string ProcessorCountVariable = "DOTNET_PROCESSOR_COUNT";
    internal const string TieredCompilationVariable = "DOTNET_TieredCompilation";
    internal const string TieredPgoVariable = "DOTNET_TieredPGO";
    internal const string LegacyTieredCompilationVariable = "COMPlus_TieredCompilation";
    internal const string LegacyTieredPgoVariable = "COMPlus_TieredPGO";
}
