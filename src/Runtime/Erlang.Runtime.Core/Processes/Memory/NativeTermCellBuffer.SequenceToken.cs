using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe partial class NativeTermCellBuffer526
{
    // Allocate every replacement handle before changing any physical cell.
    // Caller owns H. Commit only frees handles and writes native words; no
    // arbitrary term importer, native callback or user comparison is invoked.
    internal void RewriteSequenceTokenCells(
        int offset,
        Term flags,
        Term serial,
        Term sender,
        Term last
    )
    {
        int[] indices = [offset, checked(offset + 2), checked(offset + 3), checked(offset + 4)];
        Term[] values = [flags, serial, sender, last];
        var replacements = new GCHandle[indices.Length];
        int allocated = 0;
        try
        {
            for (int index = 0; index < indices.Length; index++)
            {
                Require526(indices[index]);
                if (kinds[indices[index]] != CellStorage526Names.TermKind)
                    throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
                replacements[index] = GCHandle.Alloc(values[index], GCHandleType.Normal);
                allocated++;
            }
        }
        catch
        {
            for (int index = 0; index < allocated; index++) replacements[index].Free();
            throw;
        }
        for (int index = 0; index < indices.Length; index++)
        {
            int target = indices[index];
            GCHandle old = GCHandle.FromIntPtr((nint)pointer[target]);
            pointer[target] = (nuint)GCHandle.ToIntPtr(replacements[index]);
            old.Free();
        }
        GC.KeepAlive(this);
    }
}
