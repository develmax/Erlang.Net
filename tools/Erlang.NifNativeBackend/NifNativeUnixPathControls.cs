using System.Text;

namespace Erlang.NifNativeBackend;

internal static class NifNativeUnixPathControls
{
    internal static void Run()
    {
        RawBytesFirstNulAndCopies();
        PolicyUsesExactBytes();
        AbsoluteDoesNotReadWorkingDirectory();
        BasenameKeepsLoaderSearch();
        RelativeSymlinkAndSuffixStayLexical();
    }

    private static void RawBytesFirstNulAndCopies()
    {
        byte[] cwd = [(byte)'/', (byte)'w', 0xfe];
        byte[] stem = [0xff, 0, 0xfd];
        var raw = NifNativeLibraryPath.ResolveUnix(stem).AsAbsolute(() => cwd);
        NifNativeHarness.Require(raw.UnixNullTerminated().SequenceEqual(new byte[]
            { (byte)'/', (byte)'w', 0xfe, (byte)'/', 0xff, (byte)'.', (byte)'s', (byte)'o', 0 }));
        stem[0] = 0;
        cwd[1] = 0;
        byte[] exported = raw.UnixNullTerminated();
        exported[1] = 0;
        NifNativeHarness.Require(raw.UnixNullTerminated()[1] == (byte)'w');
    }

    private static void PolicyUsesExactBytes()
    {
        var raw = NifNativeLibraryPath.ResolveUnix(new byte[] { 0xff }).AsAbsolute(() => [(byte)'/']);
        var collision = NifNativeLibraryPath.FromUnixAbsolute(Encoding.UTF8.GetBytes(raw.DisplayPath));
        NifNativeHarness.Require(collision.DisplayPath == raw.DisplayPath && !collision.SameFileName(raw));
        NifNativeHarness.Require(NifNativeLibraryPath.FromUnixAbsolute(raw.UnixNullTerminated()[..^1]).SameFileName(raw));
        var differentRaw = NifNativeLibraryPath.ResolveUnix(new byte[] { 0xfe }).AsAbsolute(() => [(byte)'/']);
        var otherRaw = NifNativeLibraryPath.ResolveUnix(new byte[] { 0xff }).AsAbsolute(() => [(byte)'/']);
        NifNativeHarness.Require(differentRaw.DisplayPath == otherRaw.DisplayPath && !differentRaw.SameFileName(otherRaw));
    }

    private static void AbsoluteDoesNotReadWorkingDirectory()
    {
        int cwdCalls = 0;

        byte[] Directory()
        {
            cwdCalls++;
            throw new InvalidOperationException(NifNativeHarnessNames.Broken);
        }
        var absolute = NifNativeLibraryPath.ResolveUnix(Encoding.UTF8.GetBytes("/absolute"));
        NifNativeHarness.Require(ReferenceEquals(absolute, absolute.AsAbsolute(Directory)) && cwdCalls == 0);
    }

    private static void BasenameKeepsLoaderSearch()
    {
        var basename = NifNativeLibraryPath.ResolveUnix(Encoding.UTF8.GetBytes("library"));
        NifNativeHarness.Require(basename.IsUnixBasename && basename.DisplayPath == "library.so");
        NifNativeHarness.Require(NifNativeCapabilityProfile.ForHostLibrary(basename).OpenLibraryFile() is null);
        NifNativeHarness.Require(!NifNativeLibraryPath.ResolveUnix(Encoding.UTF8.GetBytes("./library")).IsUnixBasename);
        NifNativeHarness.Require(NifNativeLibraryPath.ResolveUnix(Encoding.UTF8.GetBytes("./library")).DisplayPath == "./library.so");
    }

    private static void RelativeSymlinkAndSuffixStayLexical()
    {
        Require("/symlink/../name", "/cwd", "/symlink/../name.so");
        Require("symlink/../name", "/cwd", "/cwd/symlink/../name.so");
        Require("name.so", "/cwd/", "/cwd/name.so.so");
        Require("", "/cwd", "/cwd/.so");
        Require("", "/", "/.so");
        Require("name\0ignored", "/cwd", "/cwd/name.so");
        Require("café", "/cwd", "/cwd/café.so");
    }


    private static void Require(string stem, string cwd, string expected)
    {
        var path = NifNativeLibraryPath.ResolveUnix(Encoding.UTF8.GetBytes(stem)).AsAbsolute(() => Encoding.UTF8.GetBytes(cwd));
        NifNativeHarness.Require(path.UnixNullTerminated().SequenceEqual(Encoding.UTF8.GetBytes(expected).Append((byte)0)));
    }
}
