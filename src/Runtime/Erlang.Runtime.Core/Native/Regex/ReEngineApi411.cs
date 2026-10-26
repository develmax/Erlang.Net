using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang;

internal sealed class ReEngineApi411 : IDisposable
{
    private IntPtr image;
    internal ReConfig411 Config { get; }
    internal ReCompile411 Compile { get; }
    internal ReRelease411 FreeCode { get; }
    internal ReMatchData411 CreateData { get; }
    internal ReRelease411 FreeData { get; }
    internal ReMatch411 Match { get; }
    internal ReSetLoops411 SetLoops { get; }
    internal ReGetLoops411 GetLoops { get; }
    internal ReVector411 Vector { get; }
    internal RePatternInfo411 Info { get; }
    internal ReErrorMessage411 Message { get; }
    internal ReSetRestartData411 SetRestart { get; }
    internal ReSetRestartFlags411 SetFlags { get; }
    internal ReRelease411 FreeRestart { get; }
    internal ReSerializeDecode413 Decode { get; }

    internal ReEngineApi411(string admittedImage)
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) throw new PlatformNotSupportedException();
        admittedImage = Path.GetFullPath(admittedImage);
        if (Path.GetFileName(admittedImage) != ReProtocolNames411.BeamSmpDll || HashImage(admittedImage) != ReNames411.ImageHash)
            throw new InvalidOperationException(ReDiagnosticNames411.OriginalRegexEngineImageMismatch);
        string directory = Path.GetDirectoryName(admittedImage)!;

        bool Allowed(string path) => string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetDirectoryName(path), Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase);
        var tpa = ReManagedHost411.Capture();
        string[] before = Modules();
        string[] imports = [ReProtocolNames411.BeamSmpDll, ReProtocolNames411.MSVCP140Dll, ReProtocolNames411.VCRUNTIME140Dll, ReProtocolNames411.VCRUNTIME1401Dll, ReProtocolNames411.KERNEL32Dll, ReProtocolNames411.ADVAPI32Dll, ReProtocolNames411.USER32Dll, ReProtocolNames411.WS232Dll, ReProtocolNames411.IPHLPAPIDLL];
        foreach (var path in before.Where(p => imports.Contains(Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)))
            if (!Allowed(path)) throw new InvalidOperationException(ReDiagnosticNames411.ForeignRegexNativeDependency);
        if (before.Any(p => Path.GetFileName(p).Equals(ReProtocolNames411.BeamSmpDll, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(ReDiagnosticNames411.RegexEngineImageAlreadyLoadedSharedOwnershipIsNotImplemented);
        image = ReNative411.LoadLibraryExW(admittedImage, IntPtr.Zero, ReNames411.LoadFlags);
        if (image == IntPtr.Zero) throw new InvalidOperationException(ReDiagnosticNames411.RegexEngineLoadFailed + Marshal.GetLastWin32Error());
        try
        {
            // Only newly loaded native imports require native directories. Managed TPA classification is separately exact-file bound.
            foreach (var path in Modules().Except(before, StringComparer.OrdinalIgnoreCase))
                if (!Allowed(path) && !ReManagedHost411.IsBound(tpa, path)) throw new InvalidOperationException(ReDiagnosticNames411.UnexpectedRegexDependency);
            Config = Resolve<ReConfig411>(ReExportNames411.Pcre2Config8);
            Compile = Resolve<ReCompile411>(ReExportNames411.Pcre2Compile8);
            FreeCode = Resolve<ReRelease411>(ReExportNames411.Pcre2CodeFree8);
            CreateData = Resolve<ReMatchData411>(ReExportNames411.Pcre2MatchDataCreateFromPattern8);
            FreeData = Resolve<ReRelease411>(ReExportNames411.Pcre2MatchDataFree8);
            Match = Resolve<ReMatch411>(ReExportNames411.Pcre2Match8);
            SetLoops = Resolve<ReSetLoops411>(ReExportNames411.Pcre2SetLoopsLeft8);
            GetLoops = Resolve<ReGetLoops411>(ReExportNames411.Pcre2GetLoopsLeft8);
            Vector = Resolve<ReVector411>(ReExportNames411.Pcre2GetOvectorPointer8);
            Info = Resolve<RePatternInfo411>(ReExportNames411.Pcre2PatternInfo8);
            Message = Resolve<ReErrorMessage411>(ReExportNames411.Pcre2GetErrorMessage8);
            SetRestart = Resolve<ReSetRestartData411>(ReExportNames411.Pcre2SetRestartData8);
            SetFlags = Resolve<ReSetRestartFlags411>(ReExportNames411.Pcre2SetRestartFlags8);
            FreeRestart = Resolve<ReRelease411>(ReExportNames411.Pcre2FreeRestartData8);
            Decode = Resolve<ReSerializeDecode413>(ReExtendedNames413.SerializeDecode);
            if (ReadConfig(ReNativeConstants411.Unicode) != 1 || ReadConfig(ReNativeConstants411.CompiledWidths) != 0 || Version() != ReProtocolNames411.EngineVersion) throw new InvalidOperationException(ReDiagnosticNames411.UnexpectedOriginalRegexConfiguration);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private static string HashImage(string path)
    {
        using var stream = File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string[] Modules() => Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Select(p => Path.GetFullPath(p.FileName)).ToArray();

    private T Resolve<T>(string name) where T : Delegate
    {
        var pointer = ReNative411.GetProcAddress(image, name);
        if (pointer == IntPtr.Zero || !ReNative411.GetModuleHandleExW(ReNativeConstants411.ExportOwnerFlags, pointer, out var owner) || owner != image)
            throw new InvalidOperationException(ReDiagnosticNames411.MissingForwardedRegexExport + name);

        return Marshal.GetDelegateForFunctionPointer<T>(pointer);
    }

    internal uint ReadConfig(uint item)
    {
        using var buffer = new ReBuffer411(new byte[sizeof(uint)]);
        if (Config(item, buffer.Pointer) != 0) throw new InvalidOperationException(ReDiagnosticNames411.RegexConfigFailed);

        return unchecked((uint)Marshal.ReadInt32(buffer.Pointer));
    }

    internal string Version()
    {
        using var buffer = new ReBuffer411(new byte[ReNativeConstants411.VersionBufferBytes]);
        if (Config(ReNativeConstants411.Version, buffer.Pointer) < 0) throw new InvalidOperationException(ReDiagnosticNames411.RegexVersionFailed);

        return Marshal.PtrToStringUTF8(buffer.Pointer)!;
    }

    public void Dispose()
    {
        var owned = Interlocked.Exchange(ref image, IntPtr.Zero);
        if (owned != IntPtr.Zero && !ReNative411.FreeLibrary(owned)) throw new InvalidOperationException(ReDiagnosticNames411.RegexEngineUnloadFailed);
    }
}
