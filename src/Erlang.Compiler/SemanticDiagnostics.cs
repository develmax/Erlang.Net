namespace Erlang.Compiler;

internal static class SemanticDiagnostics
{
    public const string DuplicateFunction = "Duplicate function definition";
    public const string MapConstructionOperator = "Map construction requires '=>' fields; ':=' is for updates or patterns";
    public const string GuardOperator = "Operator is not legal in a guard";
    public const string GuardMatch = "Match is not legal in a guard";
    public const string GuardCase = "case is not legal in a guard";
    public const string GuardIf = "if is not legal in a guard";
    public const string GuardBlock = "begin is not legal in a guard";
    public const string GuardCatch = "catch is not legal in a guard";
    public const string GuardTry = "try is not legal in a guard";
    public const string GuardMaybe = "maybe is not legal in a guard";

    public static string BoundStacktrace(string name) => $"Stacktrace variable '{name}' must be fresh";

    public static string GuardStacktrace(string name) => $"Stacktrace variable '{name}' is not legal in a guard";

    public const string GuardReceive = "receive is not legal in a guard";
    public const string GuardFun = "fun is not legal in a guard";
    public const string FunClauseArityMismatch = "fun clauses must have equal arity";
    public const string GuardDynamicCall = "Dynamic calls are not legal in guards";

    public static string UndefinedExport(string name, int arity) => $"Undefined export {name}/{arity}";

    public static string UnsafePatternVariable(string name) => $"Unsafe pattern variable '{name}'";

    public static string UnboundOrUnsafeVariable(string name) => $"Unbound or unsafe variable '{name}'";

    public static string IllegalGuardCall(string function, int arity) => $"Illegal guard call '{function}/{arity}'";

    public static string UnsafeMatchVariable(string name) => $"Unsafe match variable '{name}'";
}
