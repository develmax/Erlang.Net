namespace Erlang.Differential;

public static class OracleInvestigationModes
{
    public const string CommandLine = "--investigate";
    public const string InitialProcess = "initial command process";
    public const string FreshProcess = "fresh child, trap_exit=false";
    public const string TrappedFreshProcess = "fresh child, trap_exit=true";
    public const string DefaultCompiler = "default native compiler";
    public const string DisabledOptimizations = "no_copt,no_ssa_opt,no_postopt";
    public const string DisabledCoreOptimization = "no_copt";
    public const string DisabledSsaOptimization = "no_ssa_opt";
    public const string DisabledPostOptimization = "no_postopt";
}
