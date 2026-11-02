namespace Erlang;

// Binding metadata, never a guest root. Actual frame/heap publication owns the
// BinRef and the owner lease; retiring that header clears this binding.
internal sealed record WritableBinaryBinding(ProcessTermHeap506 Heap, NativeOffheapHeader576 BinRef);
