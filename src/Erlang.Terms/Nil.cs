using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Nil : Term
{
    private Nil()
    {
    }
    public static Nil Value { get; } = new();
    public override string ToString() => "[]";
}
