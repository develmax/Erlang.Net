using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Pid(string node, ulong id, uint creation = 0) : Term
{
    public string Node { get; } = node;
    public ulong Id { get; } = id;
    public uint Creation { get; } = creation;

    public override string ToString() => $"<{Node}.{Id}.{Creation}>";
}
