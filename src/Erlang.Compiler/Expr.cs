using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public abstract record Expr
{
    public sealed record Literal(Term Value) : Expr;
    public sealed record Variable(string Name) : Expr;
    public sealed record Tuple(IReadOnlyList<Expr> Items) : Expr;
    public sealed record List(IReadOnlyList<Expr> Items, Expr? Tail = null) : Expr;
    public sealed record Map(Expr? Base, IReadOnlyList<MapField> Fields) : Expr;
    public sealed record Bits(IReadOnlyList<BitSegment> Segments) : Expr;
    public sealed record Call(string? Module, string Function, IReadOnlyList<Expr> Arguments) : Expr;
    public sealed record Apply(Expr Function, IReadOnlyList<Expr> Arguments) : Expr;
    public sealed record Unary(string Operator, Expr Operand) : Expr;
    public sealed record Binary(string Operator, Expr Left, Expr Right) : Expr;
    public sealed record Match(Pattern Pattern, Expr Value) : Expr;
    public sealed record Sequence(IReadOnlyList<Expr> Items) : Expr;
    public sealed record Case(Expr Value, IReadOnlyList<Clause> Clauses) : Expr;
    public sealed record If(IReadOnlyList<Clause> Clauses) : Expr;
    public sealed record Receive(IReadOnlyList<Clause> Clauses, Expr? Timeout, Expr? After) : Expr;
    public sealed record Fun(IReadOnlyList<Clause> Clauses) : Expr;
    public sealed record GuardAlternatives(IReadOnlyList<Expr> Items) : Expr;
}
