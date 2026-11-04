namespace Erlang;

// Shared prim_file boundary; each OS backend returns an Erlang errno name.
internal static class PrimFileDeleteOperations
{
    internal static Term Delete(Term path, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        string? error = OperatingSystem.IsWindows()
            ? PrimFileWindowsDelete.Delete(path)
            : OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
                ? PrimFileUnixDelete.Delete(path)
                : PrimFileDeleteNames.Unsupported;

        return error is null ? Term.A(CommonAtomNames.Ok)
            : Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(error));
    }
}
