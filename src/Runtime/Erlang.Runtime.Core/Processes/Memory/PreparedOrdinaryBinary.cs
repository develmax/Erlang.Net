namespace Erlang;

internal sealed class PreparedOrdinaryBinary : IDisposable
{
    internal OrdinaryBinaryBacking Owner { get; }
    internal BinaryBacking Backing { get; }
    internal bool CurrentWriter { get; }
    private IDisposable? interest;

    internal PreparedOrdinaryBinary(ProcessRuntime runtime, BinaryBacking source)
        : this(runtime, BitString.FromPersistentBacking416(source, 0, checked(source.Length524 * BitStringLayout.BitsPerByte)), null) { }

    internal PreparedOrdinaryBinary(ProcessRuntime runtime, BitString value, ProcessTermHeap506? destination)
    {
        BinaryBacking source = value.ResourceBacking577;
        if (source.NativeStorage524 is OrdinaryBinaryBacking existing)
        {
            if (!ReferenceEquals(existing.Runtime, runtime)) throw new NotSupportedException(NativeResource524Names.Foreign);
            WritableBinaryBinding? binding = existing.WritableBinding;
            // A foreign/store view must freeze under the SOURCE heap before any
            // target heap is held; local alias roots reuse the real BinRef.
            if (binding is not null && !ReferenceEquals(binding.Heap, destination)) existing.PinWritable();
            interest = existing.Retain();
            Owner = existing;
            Backing = source;
        }
        else
        {
            WritableBinarySnapshot? writable = source.HasWritableOrigin ? value.CaptureWritableConstruction() : null;
            Owner = writable is not null ? runtime.CaptureWritableBinary(writable) : runtime.CaptureOrdinaryBinary(source);
            CurrentWriter = writable?.CurrentWriter == true;
            interest = Owner;
            try
            {
                Backing = new BinaryBacking(Owner);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }

    public void Dispose() => Interlocked.Exchange(ref interest, null)?.Dispose();
}
