namespace Erlang;

internal static class NifNativeDiagnostics
{
    internal const string HostAndGuestFailure = "Native callback host failure with a separately retained pending Erlang exception.";
    internal const string TrustedSource = "This internal backend requires the exact reviewed fixture source and independently trusted binary hash.";
    internal const string NoDeclaredNif = "Native function is not declared on the reserved code image.";
    internal const string ForeignRuntime = "Native image binding belongs to another runtime.";
    internal const string DisposedLibrary = "Native image library owner is closed.";
    internal const string BridgeContract = "Native bridge registration, symbols or ABI contract mismatch.";
    internal const string BridgeLifetime = "A collectible managed provider cannot own process-global native exports.";
    internal const string UnsupportedPlatform = "Internal native backend supports only Windows x64 or Linux x64 glibc.";
    internal const string VariadicBoundary = "Native variadic argument count exceeds the bounded bridge profile.";
    internal const string NativeThreadUnsupported = "NIF native threads are outside this fixture ABI subset.";
    internal const string DuplicateEnvironment = "Duplicate native environment address.";
    internal const string ExpiredEnvironment = "Unknown or expired NIF environment.";
    internal const string TermHandleExhausted = "NIF term handle space exhausted.";
    internal const string ForeignTermHandle = "Unknown or foreign NIF term handle.";
    internal const string ForeignAllocationReallocate = "Foreign or stale native reallocation.";
    internal const string ForeignAllocationFree = "Foreign or duplicate native free.";
    internal const string NullIntegerOutput = "Null NIF integer output.";
    internal const string UnsupportedStringEncoding = "Fixture only supports nonnull Latin1 NIF strings.";
    internal const string StringReadBoundary = "Fixture NIF string exceeds explicit read boundary.";
    internal const string CallbackPlatform = "Pinned fixture callback table requires Windows x64.";
    internal const string UntrustedLibraryHash = "Native library does not match the trusted fixture artifact hash.";
    internal const string ModuleIdentity = "Fixture module identity mismatch.";
    internal const string ManagedLayout = "Managed NIF struct layout differs from the pinned x64 prediction.";
    internal const string NullNativeEntry = "Native fixture initialization returned null.";
    internal const string UnsupportedNativeEntry = "Native entry is outside the pinned two-function fixture ABI subset.";
    internal const string MissingFunctionName = "Missing native function name.";
    internal const string UntrustedFunction = "Native function is outside the fixture allowlist.";
    internal const string LoadVeto = "Native fixture load vetoed publication.";
}
