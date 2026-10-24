namespace Erlang.Tests;

internal static class ProcLibReportProbeNames
{
    public const string Success = "ok";
    public const string PrimitiveTest = "proc_lib report primitives match original OTP";
    public const string PrettyTest = "proc_lib pretty printer source contracts";
    public const string CallbackTest = "proc_lib report awaits its actual extended callback";
    public const string DeepUnicodeTest = "proc_lib unicode continuation handles deep empty prefixes";
    public const string SdkObservation = "SDK_OBSERVATION_ORIGINAL_COMPARISON_PENDING";
    public const string PrettyPrinterType = "Erlang.Otp.ProcLibReportPrettyPrinter";
    public const string CharactersType = "Erlang.Otp.ProcLibReportCharacters";
    public const string FlattenUnicode = "FlattenUnicode";
    public const string Write = "Write";
    public const string CallbackModule = "report_await_host";
    public const string CallbackFunction = "failed_operation";
    public const string FormatError = "format_error";
    public const string ErrorInfo = "error_info";
    public const string Module = "module";
    public const string Reason = "host_reason";
    public const string ReasonKey = "reason";
    public const string Replacement = "host replacement reason";
    public const string CallbackMismatch = "The report formatter must await and retain its real extended-error callback result.";
    public const string CallbackNotSuspended = "The report caller completed before the real extended-error callback was released.";
    public const string ProcLib = "proc_lib";
    public const string Format = "format";
    public const string CallbackAwait = "HOST_ACTUAL_EXTENDED_CALLBACK_AWAIT_CONTROL";
    public const string ControlsType = "Erlang.Otp.ProcLibReportFormatControls";
    public const string FormatMethod = "Format";
    public const string Measure = "Measure";
    public const string FormatKind = "Format";
    public const string MeasureLatin1 = "MeasureLatin1";
    public const string Latin1 = "latin1";
    public const string Unicode = "unicode";
    public const string Unlimited = "unlimited";
    public const string CharsLimit = "chars_limit";
    public const string Value = "value";
    public const string Exception = "exception";
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
