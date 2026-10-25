using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record MapPatternField(Expr Key, Pattern Value);
