namespace Erlang.NifNativeBackend;

public static class NativeRecordBeamCaseNames
{
    public const string Whole = "beam/native-record-original-get-elements-update-and-gc-whole-outcome";
    public const string Roles = "beam/native-record-original-get-elements-update-operand-guards";
    internal const string Module = "native_record_physical";
    internal const string Probe = "native_record_physical_probe";
    internal const string Observer = "native_record_physical_observer";
    internal const string Run = "run";
    internal const string ValidateRoles = "ValidateRoles";
    internal const string Failure = "Native record BEAM regression failed";
}
