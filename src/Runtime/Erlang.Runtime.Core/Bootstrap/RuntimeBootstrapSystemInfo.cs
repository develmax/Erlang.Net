namespace Erlang;

internal static class RuntimeBootstrapSystemInfo
{
    internal const string OsType = "os_type";
    internal const string WordSize = "wordsize";
    internal const string DebugCompiled = "debug_compiled";
    private const string Windows = "win32";
    private const string WindowsNt = "nt";
    private const string Unix = "unix";
    private const string Linux = "linux";
    private const string Darwin = "darwin";
    private const string FreeBsd = "freebsd";
    private const string UnsupportedHost = "The host OS has no configured OTP operating-system identity";
#if DEBUG
    private const bool DebugBuild = true;
#else
    private const bool DebugBuild = false;

#endif

    internal static bool TryRead(Term query, out Term result)
    {
        result = Nil.Value;
        if (query is Atom { Name: DebugLockNames419.LockCounting })
            result = CoreModules.Boolean(DebugLockNames419.BuildHasLockCounting);
        else if (query is Atom { Name: WordSize })
            result = Term.I(IntPtr.Size);
        else if (query is Atom { Name: DebugCompiled })
            result = CoreModules.Boolean(DebugBuild);
        else if (query is Atom { Name: RuntimeVmMetadataNames.BuildType })
            result = Term.A(DebugBuild ? RuntimeVmMetadataNames.Debug : RuntimeVmMetadataNames.Optimized);
        else if (query is Atom { Name: RuntimeVmMetadataNames.EmulatorFlavor })
            result = Term.A(RuntimeVmMetadataNames.Interpreter);
        else if (query is Atom { Name: RuntimeVmMetadataNames.OtpRelease })
            result = Term.String(RuntimeDriverFrontendOptionNames424.OtpContractRelease);
        else if (query is Atom { Name: OsType })
            result = OperatingSystem.IsWindows() ? Term.Tuple(Term.A(Windows), Term.A(WindowsNt))
                : OperatingSystem.IsLinux() ? Term.Tuple(Term.A(Unix), Term.A(Linux))
                : OperatingSystem.IsMacOS() ? Term.Tuple(Term.A(Unix), Term.A(Darwin))
                : OperatingSystem.IsFreeBSD() ? Term.Tuple(Term.A(Unix), Term.A(FreeBsd))
                : throw new PlatformNotSupportedException(UnsupportedHost);
        else
            return false;

        return true;
    }
}
