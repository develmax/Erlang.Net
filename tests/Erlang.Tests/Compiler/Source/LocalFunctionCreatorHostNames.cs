namespace Erlang.Tests;

internal static class LocalFunctionCreatorHostNames
{
    internal const string CanonicalizationTest = "serialization/local-fun-foreign-creator-canonicalizes-to-init";
    internal const string ValidationTest = "serialization/local-fun-invalid-creator-rejected-before-factory";
    internal const string Module = "local_fun_creator371_stub";
    internal const string ForeignNode = "creator371@external";
    internal const string OtherForeignNode = "other371@external";
    internal const string Erlang = "erlang";
    internal const string FunInfo = "fun_info";
    internal const string Pid = "pid";
    internal const string InvalidCreator = "invalid_creator";
    internal const string Normal = "normal";
    internal const string Failure = "Local FUN creator handling differs from the pinned OTP initialization-PID contract.";
}
