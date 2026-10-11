namespace Erlang.Tests;

internal static class LocalFunctionCheckDiagnostics
{
    public const string LocalExactEqualityHashCoherence = "local exact equality/hash coherence";
    public const string ActualFreeEnvironment = "actual free environment";
    public const string MutableExposedEnvironment = "mutable exposed environment";
    public const string IndependentCanonicalCodeIdentity = "independent canonical code identity";
    public const string InvokingVMInitializationOwner = "invoking VM initialization owner";
    public const string CrossContextLocalInvocation = "cross-context local invocation";
    private const string OriginExecutionPrefix = "origin execution ";
    private const string TargetExecutionPrefix = "target execution ";

    public static string OriginExecution(Term reason) => OriginExecutionPrefix + reason;

    public static string TargetExecution(Term reason) => TargetExecutionPrefix + reason;
}
