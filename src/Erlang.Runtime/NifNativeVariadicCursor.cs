namespace Erlang;

internal static unsafe class NifNativeVariadicCursor
{
    internal static delegate* unmanaged[Cdecl]<nint, nuint*, int> Reader;

    internal static Term[] Read(NifNativeCallEnvironment environment, uint count, nint cursor)
    {
        if (count > NifNativeNames.MaximumVariadicItems || Reader == null)
            throw new NotSupportedException(NifNativeDiagnostics.VariadicBoundary);
        var items = new Term[checked((int)count)];
        for (int index = 0; index < items.Length; index++)
        {
            nuint handle;
            if (Reader(cursor, &handle) != 1)
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            items[index] = environment.Get(handle);
        }

        return items;
    }
}
