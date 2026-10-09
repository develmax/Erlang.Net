namespace Erlang;

/// <summary>Canonical atoms used as Erlang exception reasons or reason tuple tags.</summary>
public static class ErlangErrorReasons
{
    public const string BadArgument = "badarg";
    public const string BadArithmetic = "badarith";
    public const string BadArity = "badarity";
    public const string BadFunction = "badfun";
    public const string BadKey = "badkey";
    public const string BadMap = "badmap";
    public const string BadMatch = "badmatch";
    public const string CaseClause = "case_clause";
    public const string FunctionClause = "function_clause";
    public const string SystemLimit = "system_limit";
    public const string TimeoutValue = "timeout_value";
    public const string UndefinedFunction = "undef";
}
