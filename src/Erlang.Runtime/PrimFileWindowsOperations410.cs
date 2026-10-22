namespace Erlang;

internal static class PrimFileWindowsOperations410
{
    internal static void Require(CancellationToken cancellation)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(PrimFileWindowsNames410.UnsupportedPlatform);
        cancellation.ThrowIfCancellationRequested();
    }

    internal static Term GetCwd(CancellationToken cancellation)
    {
        Require(cancellation);
        var path = new char[PrimFileWindowsNames410.MaxPath];
        if (PrimFileWindowsNative410.WorkingDirectory(0, path, path.Length) == IntPtr.Zero)
            return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.Denied);
        int length = Array.IndexOf(path, '\0');
        if (length < 0) throw new InvalidOperationException();

        return PrimFileWindowsErrors410.Ok(new BitString(PrimFileWindowsPaths410.Bytes(
            PrimFileWindowsPaths410.NormalizeResult(new string(path, 0, length)))));
    }

    internal static Term ReadFile(Term path, CancellationToken cancellation)
        => PrimFileWindowsRead410.Run(path, cancellation);

    internal static Term ReadInfo(Term path, Term follow, CancellationToken cancellation)
        => PrimFileWindowsInfo410.Run(path, follow, cancellation);

    internal static Term ListDir(Term path, CancellationToken cancellation)
        => PrimFileWindowsDirectory410.Run(path, cancellation);
}
