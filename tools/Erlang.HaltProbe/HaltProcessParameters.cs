namespace Erlang.HaltProbe;

internal static class HaltProcessParameters
{
    public const string SourceDirectory = "sources";
    public const string EvidenceFile = "probe-evidence.json";
    public const string NativeStageFile = "native-stage.txt";
    public const string RawStandardOutputFile = "process-stdout.raw";
    public const string RawStandardErrorFile = "process-stderr.raw";
    public const string RawProcessEvidenceFile = "process-raw-evidence.json";
    public const int StreamCaptureBufferBytes = 4096;
    public const string TerminationFailure = "Could not terminate the owned halt subprocess";
    public const string DumpFile = "requested-clr.dmp";
    public const string CompileStage = "source-compile";
    public const string ExecuteStage = "entry-execute";
    public const string ReturnStage = "entry-returned";
    public const string ProcessExited = "process-exited";
    public const string TimedOut = "external-probe-timeout";
    public const string NoEvidence = "missing-sdk-probe-evidence";
    public const int ProcessTimeoutSeconds = 30;
    public const string CompileFailureVariablePrefix = "CompileFailure_";
}
