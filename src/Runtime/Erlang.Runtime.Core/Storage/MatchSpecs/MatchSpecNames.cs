namespace Erlang;

public static class MatchSpecNames
{
    public const string Test = "match_spec_test";
    public const string Table = "table";
    public const string Trace = "trace";
    public const string Whole = "$_";
    public const string Variables = "$$";
    public const string Wildcard = "_";
    public const string Constant = "const";
    public const string Self = "self";
    public const string Message = "message";
    public const string ReturnTrace = "return_trace";
    public const string ExceptionTrace = "exception_trace";
    public const string Caller = "caller";
    public const string Exit = "EXIT";
    public const string Undefined = "undefined";
    public const string Ok = "ok";
    public const string Unsupported = "not_supported";
    public const string UnsupportedOperation = "match_spec_operation";
    public const int MaximumDepth = 512;
    public const int MaximumConstructedMapEntries = 32;

    public static ErlangException UnsupportedAction(string name, int arity) => new(Term.Tuple(
        Term.A(Unsupported),
        Term.Tuple(Term.A(UnsupportedOperation), Term.A(name), Term.I(arity))
    ));
}
