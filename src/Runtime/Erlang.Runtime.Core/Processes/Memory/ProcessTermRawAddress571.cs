namespace Erlang;

// An internal, revalidated view of an already registered block, not ownership.
// The address may be consumed synchronously only while Owner.Gate stays held.
// Holding this value never roots a block or extends its physical lifetime.
internal readonly struct ProcessTermRawAddress571(
    ProcessTermHeap506 owner,
    ProcessTermSequence506 block,
    int offset,
    int words
)
{
    internal nint Address571 => owner.ReadRawAddress571(block, offset, words);
}
