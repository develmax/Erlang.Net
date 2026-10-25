namespace LoaderIodataContrasts;

internal static class LoaderArtifactPaths
{
    internal const string ReleasesDirectory = "releases";
    internal const string InstalledVersionFile = "OTP_VERSION";
    internal const string NativeExecutableDirectory = "bin";
    internal const string RuntimeExecutable = "erl";
    internal const string CompilerExecutable = "erlc";
    internal const string WindowsExecutableSuffix = ".exe";
    internal const string OtherExecutableSuffix = "";
    internal const string CapturedBuildEvidence = "source-build-evidence.json";
    internal const string CapturedBuildLog = "source-build.log";
    internal const string PinnedSourceProvenance = "pinned-source-provenance.json";
    internal const string InputsDirectory = "inputs";
    internal const string InvokedRuntimeText = "invoked-runtime.txt";
    internal const string InvokedRuntimeEtf = "invoked-runtime.etf";
    internal const string RuntimeEvaluation = "invoked-runtime-evaluation.erl";
    internal const string RuntimeCapturePrefix = "runtime-version";
    internal const string CompileCapturePrefix = "compile";
    internal const string BeamFileSuffix = ".beam";
    internal const string DisassemblyText = "wrapper-disassembly.txt";
    internal const string DisassemblyEvaluation = "disassembly-evaluation.erl";
    internal const string DisassemblyCapturePrefix = "disassemble";
    internal const string CaseEvaluation = "evaluation.erl";
    internal const string CaseStage = "stage.txt";
    internal const string CaseProcessPrefix = "process";
    internal const string CaseActualEtf = "actual.etf";
    internal const string CaseExpectedEtf = "expected.etf";
    internal const string TemporaryReportSuffix = ".tmp";
}
