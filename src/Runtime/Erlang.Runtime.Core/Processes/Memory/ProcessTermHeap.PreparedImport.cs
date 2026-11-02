namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal PreparedHeapImport PrepareHeapImport(ProcessTermFrame506 caller, Term portable)
        => PrepareHeapImportWithLiteral801(caller, portable, null);

    internal PreparedHeapImport PrepareHeapImportWithLiteral801(ProcessTermFrame506 caller, Term portable, ModuleLiteralRoots801? carriedLiteral801)
    {
        if (Monitor.IsEntered(Gate)) throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        using ProcessTermBorrow506 borrow = BorrowWithLiteral801(caller, portable, carriedLiteral801);
        using PreparedMapGraph517 maps = PrepareMapGraph517(portable, false);
        using PreparedMapGraphUse517 admitted = maps.AdmitBuilds517();
        ProcessTermFrame506 staging = Enter();
        try
        {
            ProcessTermConstruction514 custody = staging.BeginConstruction514(portable);
            Term value = ImportPrepared(
                portable,
                maps,
                custody,
                admitted
            );

            return new PreparedHeapImport(staging, value);
        }
        catch
        {
            staging.Dispose();
            throw;
        }
    }
}
