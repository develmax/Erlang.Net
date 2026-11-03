namespace Erlang;

public sealed partial class BitString
{
    // Inspection must not pin/revoke a live append writer.
    internal bool HasReferenceStorage425
    {
        get
        {
            ValidateOwnedBits577();

            return backing.IsReference;
        }
    }
}
