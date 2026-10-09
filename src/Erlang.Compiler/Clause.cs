using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record Clause(IReadOnlyList<Pattern> Patterns, Expr? Guard, Expr Body);
