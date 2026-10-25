using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record FunctionDefinition(string Name, int Arity, IReadOnlyList<Clause> Clauses);
