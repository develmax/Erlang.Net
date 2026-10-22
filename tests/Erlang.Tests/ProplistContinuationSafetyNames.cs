namespace Erlang.Tests;

internal static class ProplistContinuationSafetyNames
{
    internal const string SourceFile = "continuation_safety.erl";
    internal const string Module = "continuation_safety";
    internal const string Getter = "terminal_fun";
    internal const string Opaque = "opaque_call";
    internal const string Static = "static_named";
    internal const string Alias = "static_alias";
    internal const string Caught = "protected_catch";
    internal const string After = "protected_after";
    internal const string Captured = "captured";
    internal const string Branch = "branch";
    internal const string TerminalReason = "terminal_marker";
    internal const string HostReturned = "host_returned";
    internal const string AfterCall = "after_call";
    internal const string CaughtValue = "caught";
    internal const string AfterValue = "executed";
    internal const string GenuineCheck = "genuine-host-argument-preserves-caller";
    internal const string ForgedCheck = "host-same-identity-return-keeps-continuation";
    internal const string StaticCheck = "static-target-elides-caller";
    internal const string AliasCheck = "static-alias-elides-caller";
    internal const string CatchCheck = "catch-executes";
    internal const string AfterCheck = "after-executes";
    internal const string AstPrefix = "ast:";
    internal const string UnexpectedReturn = "Expected terminal error, invocation returned.";
    internal const string WrongReason = "Terminal reason differs.";
    internal const string WrongCaller = "Caller frame presence differs.";
    internal const string WrongValue = "Returned value differs.";
    internal const string WrongFlags = "Continuation flags differ.";
    internal const string MissingFunction = "Compiler did not return a local function.";
}
