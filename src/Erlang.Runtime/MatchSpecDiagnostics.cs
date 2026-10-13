namespace Erlang;

public static class MatchSpecDiagnostics
{
    public const string ProgramsNotList = "Match programs are not in a list.";
    public const string ProgramsNotProper = "Match programs are not in a proper list.";
    public const string ClauseArity = "Match program part is not a tuple of arity 3.";
    public const string ExpressionsNotList = "Match expression is not a list.";
    public const string ExpressionsNotProper = "Match expression is not a proper list.";
    public const string VariableMapKey = "Variable found in map key.";
    public const string WildcardMapKey = "Underscore found in map key.";
    public const string InvalidHead = "Match head is invalid in this context.";

    public static string Unbound(int variable) => $"Variable ${variable} is unbound.";

    public static string TraceHead(Term term) => $"Match expression part {term} is not a proper list.";

    public static string InvalidTuple(Term term) => $"{term} is neither a function call, nor a tuple (tuples are written {{{{ ... }}}}).";

    public static string Unknown(string name, int arity) => $"Function {Term.A(name)}/{arity} does_not_exist.";

    public static string Context(string name, int arity) => $"Function {Term.A(name)}/{arity} cannot be called in this context.";

    public static string Dialect(string name) => $"Special form '{name}' used in wrong dialect.";

    public static string Guard(string name) => $"Special form '{name}' called in guard context.";

    public static string ArgumentCount(string name, Term term) => $"Special form '{name}' called with wrong number of arguments in {term}.";

    public static string NoArguments(string name, Term term) => $"Special form '{name}' called with arguments in {term}.";

    public static string EmptyBoolean(string name, Term term) => $"Special form '{name}' called without arguments in {term}.";

    public static string ConstantArguments(Term term) => $"Special form 'const' called with more than one argument in {term}.";
}
