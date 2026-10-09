using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Atom(string name) : Term
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public override string ToString() => Name.Length > 0 && char.IsLower(Name[0]) && Name.All(c => char.IsLetterOrDigit(c) || c is '_' or '@') ? Name : "'" + Name.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
}
