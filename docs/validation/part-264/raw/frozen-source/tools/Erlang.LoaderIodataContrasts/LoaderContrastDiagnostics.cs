namespace LoaderIodataContrasts;

internal static class LoaderContrastDiagnostics
{
    internal const string Usage = "Usage: Erlang.LoaderIodataContrasts installedOtpRoot repositoryRoot newOutput buildEvidence";
    internal const string PinnedBuildRequired = "Verified pinned source-build evidence is required";
    internal const string BuildLogChanged = "Pinned source build log changed";
    internal const string InstalledVersionDiffers = "Installed OTP version differs";
    internal const string NativeExecutablesRequired = "Installed erl and erlc are required";
    internal const string NewOutputRequired = "Output directory must be new";
    internal const string SixteenScenariosRequired = "Sixteen independent scenarios required";
    internal const string ImmutableInputChangedPrefix = "Immutable native input changed: ";
    internal const string InvokedRuntimeDiffers = "Actual invoked runtime differs from the pinned JIT build; raw version evidence retained";
    internal const string WrapperCompileUnavailable = "Compiled original iodata wrapper unavailable; raw compile evidence retained";
    internal const string WrapperDisassemblyMissing = "Actual compiled iodata wrapper disassembly missing";
    internal const string NativeContrastsFailed = "Native iodata loader contrasts failed; every raw VM outcome retained";
    internal const string ProcessDidNotStart = "Loader oracle did not start";
}
