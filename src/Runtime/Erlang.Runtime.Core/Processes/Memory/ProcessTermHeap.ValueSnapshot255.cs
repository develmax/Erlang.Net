namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal BitString CopyHeapValue255(ProcessTermSequence506 block)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using ModuleLiteralRead800 read = domain.EnterRead800(block);

            return CopyHeapValueCore255(block);
        }
        lock (Gate)
        {
            return CopyHeapValueCore255(block);
        }
    }

    private BitString CopyHeapValueCore255(ProcessTermSequence506 block)
    {
        int bits = RequireHeapBitLength(block);
        byte[] bytes = CopyLiteralHeapBytesCore800(block);

        return new BitString(bytes, bits);
    }

    internal BitString CopyResourceValue255(ProcessTermSequence506 block)
    {
        IDisposable? copyRead = null;
        try
        {
            if (LiteralDomain800 is { IsSealed800: true } domain)
            {
                using ModuleLiteralRead800 read = domain.EnterRead800(block);
                ResourceBinaryDescriptor577 descriptor = RequireImmutableModuleDescriptor805(block);
                using ModuleLiteralRead800 referenceRead = domain.EnterRead800(descriptor.BinRef.Block);
                // Existing immutable validation checks the genuine SubBits,
                // BinRef, runtime/backing binding and complete source range.
                ResourceBitsRead577 range = ReadImmutableModuleBinary805(block, domain);
                copyRead = descriptor.Backing.RetainNativeCopyRead524();

                return descriptor.Backing.CopyValueBits255(range.BitOffset, range.BitLength);
            }
            lock (Gate)
            {
                ResourceBinaryDescriptor577 descriptor = RequireResourceBitsDescriptor577(block);
                ResourceBitsRead577 range = ReadResourceBits577(block);
                INativeBinaryStorage524 native = descriptor.Backing.NativeStorage524
                    ?? throw new InvalidOperationException(ResourceBinary577Names.Invalid);
                if (descriptor.BinRef.OrdinaryBinary is { } ordinary)
                {
                    if (!ReferenceEquals(native, ordinary))
                        throw new InvalidOperationException(ResourceBinary577Names.Invalid);
                }
                else if (!ReferenceEquals(native.ResourceIdentity524, descriptor.BinRef.Payload))
                    throw new InvalidOperationException(ResourceBinary577Names.Invalid);
                copyRead = descriptor.Backing.RetainNativeCopyRead524();

                return descriptor.Backing.CopyValueBits255(range.BitOffset, range.BitLength);
            }
        }
        finally
        {
            // An actual native release/destructor must never run under H or
            // while the immutable module's physical reader is held.
            copyRead?.Dispose();
        }
    }
}
