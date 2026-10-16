namespace Erlang.Otp;

internal static class ProcLibReportExceptionFormatNames
{
    public const string ErrorInfo = "error_info";
    public const string Module = "module";
    public const string Function = "function";
    public const string FormatError = "format_error";
    public const string Reason = "reason";
    public const string General = "general";
    public const string File = "file";
    public const string Line = "line";
    public const string ErrorPrefix = "exception error: ";
    public const string ExitPrefix = "exception exit: ";
    public const string ThrowPrefix = "exception throw: ";
    public const string StackIndent = "      ";
    public const string ReasonIndent = "    ";
    public const string ThreeSpaces = "   ";
    public const string CalledAs = "   called as ";
    public const string InOperator = "in operator ";
    public const string InFunction = "in function ";
    public const string InCallFrom = "in call from";
    public const string GeneralErrorPrefix = "*** ";
    public const string ArgumentPrefix = "*** argument ";
    public const string ColonSpace = ": ";
    public const string UndefinedFunction = "undefined function ";
    public const string UndefinedShell = "undefined shell command ";
    public const string NoFunctionClause = "no function clause matching ";
    public const string NoShellClause = "no function clause matching call to ";
    public const string CalledWith = " called with ";
    public const string NoArguments = "no arguments";
    public const string OneArgument = "one argument";
    public const string TwoArguments = "two arguments";
    public const string Arguments = " arguments";
    public const string InterpretedFunction = "interpreted function with arity ";
    public const string FunctionInformation = "fun_info";
    public const string Name = "name";
    public const string Arity = "arity";
    public const string Type = "type";
    public const string Local = "local";
    public const string UnboundPrefix = "variable ";
    public const string UnboundSuffix = " is unbound";
    public const string BadFieldPrefix = "bad field name: ";
    public const string MissingValuePrefix = "no value provided for field ";
    public const string RecordIn = " in #";
    public const string RecordSuffix = "{}";
    public const string RestrictedBadReturn = "restricted_shell_bad_return";
    public const string RestrictedDisallowed = "restricted_shell_disallowed";
    public const string RestrictedStarted = "restricted_shell_started";
    public const string RestrictedStopped = "restricted_shell_stopped";
    public const string RestrictedBadReturnPrefix = "restricted shell module returned bad value ";
    public const string RestrictedDisallowedPrefix = "restricted shell does not allow ";
    public const string RestrictedStartedText = "restricted shell starts now";
    public const string RestrictedStoppedText = "restricted shell stopped";
    public const string CallingSelf = "calling_self";
    public const string CallingSelfText = "the current process attempted to call itself";
    public const string ArgumentLimit = "argument_limit";
    public const string ArgumentLimitText = "limit of number of arguments to interpreted function exceeded";
    public const string BadField = "badfield";
    public const string NoValue = "novalue";
    public const string ShellUndefined = "shell_undef";
    public const string Unbound = "unbound";
    public const string ProbeFirst = "a2345";
    public const string ProbeSecond = "b2345";
    public const char ProbeAtomCharacter = 'a';
    public const string Slash = "/";
    public const string Colon = ":";
    public const string Space = " ";
    public const string Newline = "\n";
    public const string LocationControl = "(~ts:~ts)";
    public const string PairTextControl = "~ts~ts";
    public const string Latin1PairTextControl = "~s~s";
    public const string Latin1UnicodePairTextControl = "~s~ts";
    public const string ArityFrameControl = "~s~s ~ts ~ts~ts";
    public const string ArgumentsFrameControl = "~s~s ~ts\n~s~ts~ts";
    public const int ExceptionColumn = 5;
    public const int StackBudgetDivisor = 3;
    public const int Unlimited = -1;
    public const int DefaultWidth = 80;
    public const string MfaControl = "~ts/~w";
    public const string QualifiedFunctionControl = "~w:~ts";
    public const string WriteControl = "~w";
    public const string ShellFunctionControl = "~w/~w";
    public const string ShellUndefinedControl = "undefined shell command ~ts/~w";
    public const string BadArityControl = "~ts called with ~s";
    public const string ArgumentCountControl = "~w arguments";
    public const string RecordControl = "~w in #~w:~w{}";
    public const string ArgumentLabelControl = "*** argument ~w: ";
    public const string GeneralErrorControl = "*** ~ts";
    public const string UnaryOperatorControl = "~s ";

    public static readonly IReadOnlyDictionary<string, string> SimpleReasons = new Dictionary<string, string>
    {
        [ErlangErrorReasons.BadArgument] = "bad argument",
        [ErlangErrorReasons.BadArithmetic] = "an error occurred when evaluating an arithmetic expression",
        [ErlangErrorReasons.IfClause] = "no true branch found when evaluating an if expression",
        ["noproc"] = "no such process or port",
        ["notalive"] = "the node cannot be part of a distributed system",
        ["system_limit"] = "a system limit has been reached",
        ["timeout_value"] = "bad receive timeout value"
    };

    public static readonly IReadOnlyDictionary<string, string> ValueReasons = new Dictionary<string, string>
    {
        [ErlangErrorReasons.BadArgument] = "bad argument: ",
        [ErlangErrorReasons.BadKey] = "bad key: ",
        [ErlangErrorReasons.BadMap] = "bad map: ",
        [ErlangErrorReasons.BadFunction] = "bad function ",
        [ErlangErrorReasons.BadMatch] = "no match of right hand side value ",
        [ErlangErrorReasons.CaseClause] = "no case clause matching ",
        ["else_clause"] = "no else clause matching ",
        [ErlangErrorReasons.TryClause] = "no try clause matching ",
        [ErlangErrorReasons.BadFilter] = "bad filter ",
        [ErlangErrorReasons.BadGenerator] = "bad generator ",
        ["bad_generators"] = "bad generators: "
    };
}
