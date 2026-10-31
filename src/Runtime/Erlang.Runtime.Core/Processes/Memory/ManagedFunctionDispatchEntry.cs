using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class ManagedFunctionDispatchEntry : IDisposable
{
    internal bool Published { get; set; }
    private nint address;
    private GCHandle target;
    internal nint Address => address != 0 ? address : throw new ObjectDisposedException(nameof(ManagedFunctionDispatchEntry));

    internal ManagedFunctionDispatchEntry(FunctionTerm function)
    {
        target = GCHandle.Alloc(function);
        try
        {
            address = (nint)NativeMemory.AllocZeroed((nuint)sizeof(ManagedFunctionDispatchHeader));
            if (address == 0) throw new OutOfMemoryException();
            var header = (ManagedFunctionDispatchHeader*)address;
            header->Target = GCHandle.ToIntPtr(target);
            header->Signature = HeapFunctionLayout.DispatchSignature;
        }
        catch
        {
            target.Free();
            throw;
        }
    }

    internal FunctionTerm Resolve()
    {
        var header = (ManagedFunctionDispatchHeader*)Address;
        if (header->Signature != HeapFunctionLayout.DispatchSignature || header->Target != GCHandle.ToIntPtr(target)
            || target.Target is not FunctionTerm function)
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);

        return function;
    }

    public void Dispose()
    {
        nint selected = Interlocked.Exchange(ref address, 0);
        if (selected == 0) return;
        target.Free();
        NativeMemory.Free((void*)selected);
    }
}
