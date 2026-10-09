using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record MapField(Expr Key, Expr Value, bool Exact);
