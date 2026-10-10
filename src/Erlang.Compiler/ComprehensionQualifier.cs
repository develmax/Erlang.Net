namespace Erlang.Compiler;

public abstract record ComprehensionQualifier
{
    public sealed record Generator(Pattern Pattern, Expr Source, bool Strict = false) : ComprehensionQualifier;
    public sealed record Filter(Expr Expression) : ComprehensionQualifier;
}
