namespace Erlang;

// Strong storage handle only; no BitString wrapper or exported memory retained.
internal sealed class BinaryPayloadHandle512(BinaryBacking backing)
{
    internal object Identity => backing.NativeIdentity524 ?? backing;
    internal object? NativeIdentity524 => backing.NativeIdentity524;
    internal bool ReferenceCounted => backing.IsReference;
    internal long CapacityBytes => backing.ReferencedCapacity524;
}
