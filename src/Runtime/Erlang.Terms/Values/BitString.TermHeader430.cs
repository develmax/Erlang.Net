namespace Erlang;

public sealed partial class BitString
{
    // Inspect the actual owner/view representation, without pinning a writer,
    // slicing, normalizing, copying payload, or inferring storage from length.
    internal bool HasSubBitsStorage430
    {
        get
        {
            ValidateOwnedBits577();

            return backing.IsReference;
        }
    }
}
