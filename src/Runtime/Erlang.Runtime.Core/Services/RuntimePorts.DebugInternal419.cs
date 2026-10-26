using System.Numerics;
namespace Erlang;

internal sealed partial class RuntimePorts
{
    internal Term DebugNextIdentity419
    {
        get
        {
            lock (gate) return new Integer(new BigInteger(unchecked(nextId + 1)));
        }
    }
}
