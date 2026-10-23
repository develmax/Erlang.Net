using System.Text;

namespace Erlang;

internal sealed class NifNativeLibraryPath
{
    private readonly byte[]? unixBytes;

    internal string DisplayPath { get; }

    private NifNativeLibraryPath(string displayPath, byte[]? unixBytes)
    {
        DisplayPath = displayPath;
        this.unixBytes = unixBytes;
    }

    internal static NifNativeLibraryPath FromHostPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return OperatingSystem.IsWindows()
            ? new(path, null) : FromUnixAbsolute(Encoding.UTF8.GetBytes(path));
    }

    internal static NifNativeLibraryPath FromUnixAbsolute(ReadOnlySpan<byte> path)
    {
        if (path.IsEmpty || path[0] != NifNativeUnixPathNames.Separator || path.Contains((byte)0))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);

        return new(Encoding.UTF8.GetString(path), path.ToArray());
    }

    internal bool IsUnixBasename => unixBytes is not null && !unixBytes.Contains(NifNativeUnixPathNames.Separator);

    // Preserve dlopen lexical search: bare names remain bare; relative slash paths remain relative.
    internal static NifNativeLibraryPath ResolveUnix(ReadOnlySpan<byte> stem)
    {
        int end = stem.IndexOf((byte)0);
        if (end >= 0)
            stem = stem[..end];
        byte[] suffix = Encoding.UTF8.GetBytes(NifNativeDefaultLoaderNames.UnixSuffix);
        byte[] path = new byte[checked(stem.Length + suffix.Length)];
        stem.CopyTo(path);
        suffix.CopyTo(path.AsSpan(stem.Length));

        return new(Encoding.UTF8.GetString(path), path);
    }

    // Only legacy display resolution and reviewed slash-relative admission need an absolute identity.
    internal NifNativeLibraryPath AsAbsolute(Func<byte[]> workingDirectory)
    {
        if (unixBytes is null || unixBytes[0] == NifNativeUnixPathNames.Separator)
            return this;
        byte[] directory = workingDirectory();
        if (directory.Length == 0 || directory[0] != NifNativeUnixPathNames.Separator || directory.Contains((byte)0))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);
        bool separator = directory[^1] != NifNativeUnixPathNames.Separator;
        int prefix = directory.Length + (separator ? 1 : 0);
        byte[] path = new byte[checked(prefix + unixBytes.Length)];
        directory.CopyTo(path, 0);
        if (separator)
            path[directory.Length] = NifNativeUnixPathNames.Separator;
        unixBytes.CopyTo(path, prefix);

        return FromUnixAbsolute(path);
    }

    internal byte[] UnixNullTerminated()
    {
        if (unixBytes is null)
            throw new PlatformNotSupportedException(NifNativeDiagnostics.UnsupportedPlatform);
        byte[] result = new byte[checked(unixBytes.Length + 1)];
        unixBytes.CopyTo(result, 0);

        return result;
    }

    internal bool SameFileName(NifNativeLibraryPath other)
    {
        if (unixBytes is not null || other.unixBytes is not null)
            return unixBytes is not null && other.unixBytes is not null && unixBytes.AsSpan().SequenceEqual(other.unixBytes);

        return string.Equals(DisplayPath, other.DisplayPath, StringComparison.OrdinalIgnoreCase);
    }
}
