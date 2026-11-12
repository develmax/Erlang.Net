namespace Erlang.Compiler;

internal sealed record DocumentationSourceForm(string Name, Term Value, Expr.Call? EquivalentCall = null);
