namespace Erlang;

public sealed partial class BitString
{
    // Inspection must not pin/revoke a live append writer.
    internal bool HasReferenceStorage425 => backing.IsReference;
}
