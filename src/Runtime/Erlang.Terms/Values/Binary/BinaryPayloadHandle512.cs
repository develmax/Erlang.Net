namespace Erlang;

// Strong storage handle only; no BitString wrapper or exported memory retained.
internal sealed class BinaryPayloadHandle512(BinaryBacking backing)
{
    internal object Identity => backing;
    internal bool ReferenceCounted => backing.IsReference;
    internal long CapacityBytes => backing.Buffer.LongLength;
}
