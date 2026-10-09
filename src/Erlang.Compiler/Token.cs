using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record Token(string Kind, string Text, int Start, int End);
