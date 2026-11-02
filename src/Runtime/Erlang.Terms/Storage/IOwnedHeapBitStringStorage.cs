namespace Erlang;

internal interface IOwnedHeapBitStringStorage : IOwnedTermSequence506
{
    int ReadHeapBitLength();

    byte ReadHeapByte(int index);

    byte[] CopyHeapBytes();

    BitString CopyHeapValue255();
}
