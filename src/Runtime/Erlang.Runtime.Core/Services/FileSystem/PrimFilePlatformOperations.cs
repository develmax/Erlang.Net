namespace Erlang;

internal static class PrimFilePlatformOperations
{
    internal static Term GetCwd(CancellationToken cancellation) => OperatingSystem.IsWindows()
        ? PrimFileWindowsOperations410.GetCwd(cancellation) : PrimFilePosixOperations.GetCwd(cancellation);

    internal static Term ReadFile(Term path, CancellationToken cancellation) => OperatingSystem.IsWindows()
        ? PrimFileWindowsOperations410.ReadFile(path, cancellation) : PrimFilePosixOperations.ReadFile(path, cancellation);

    internal static Term ReadInfo(Term path, Term follow, CancellationToken cancellation) => OperatingSystem.IsWindows()
        ? PrimFileWindowsOperations410.ReadInfo(path, follow, cancellation) : PrimFilePosixOperations.ReadInfo(path, follow, cancellation);

    internal static Term ListDir(Term path, CancellationToken cancellation) => OperatingSystem.IsWindows()
        ? PrimFileWindowsOperations410.ListDir(path, cancellation) : PrimFilePosixOperations.ListDir(path, cancellation);
}
