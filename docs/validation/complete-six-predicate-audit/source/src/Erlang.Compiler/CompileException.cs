using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed class CompileException(string code, string message, int offset) : Exception(message)
{
    public string Code { get; } = code;
    public int Offset { get; } = offset;
}
