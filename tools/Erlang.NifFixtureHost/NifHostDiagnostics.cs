namespace Erlang.NifFixtureHost;

internal static class NifHostDiagnostics
{
    internal const string NativeThreadUnsupported = "NIF native threads are outside this fixture ABI subset.";
    internal const string DuplicateEnvironment = "Duplicate native environment address.";
    internal const string ExpiredEnvironment = "Unknown or expired NIF environment.";
    internal const string TermHandleExhausted = "NIF term handle space exhausted.";
    internal const string ForeignTermHandle = "Unknown or foreign NIF term handle.";
    internal const string Usage = "Pass no arguments for internal checks, or absolute DLL path plus trusted fixture SHA256.";
    internal const string AssertionFailed = "NIF prerequisite assertion failed.";
    private const string ExpectedExceptionPrefix = "Expected exception was not observed: ";

    internal static string ExpectedException(Type exceptionType) => ExpectedExceptionPrefix + exceptionType.Name;

    internal const string RetiredGenerationBinding = "NIF code generation";
    internal const string RetiredGenerationAcquisition = "NIF code generation";
    internal const string ForeignSourceOwner = "NIF caller does not own the executing image.";
    internal const string OldGenerationLoad = "Old NIF code generation cannot load a library.";
    internal const string UnbalancedGenerationLease = "Unbalanced NIF generation lease.";
    internal const string ForeignAllocationFree = "Foreign or duplicate native free.";
    internal const string NullIntegerOutput = "Null NIF integer output.";
    internal const string UnsupportedStringEncoding = "Fixture only supports nonnull Latin1 NIF strings.";
    internal const string StringReadBoundary = "Fixture NIF string exceeds explicit read boundary.";
    internal const string CallbackPlatform = "Pinned fixture callback table requires Windows x64.";
    internal const string LibraryPlatform = "This bootstrap implements only Windows x64 pinned fixture callbacks.";
    internal const string AbsoluteLibraryPath = "An absolute fixture DLL path is required.";
    internal const string UntrustedLibraryHash = "Native library does not match the trusted fixture artifact hash.";
    internal const string ModuleIdentity = "Fixture module identity mismatch.";
    internal const string ManagedLayout = "Managed NIF struct layout differs from the pinned x64 prediction.";
    internal const string NullNativeEntry = "Native fixture initialization returned null.";
    internal const string UnsupportedNativeEntry = "Native entry is outside the pinned two-function fixture ABI subset.";
    internal const string MissingFunctionName = "Missing native function name.";
    internal const string UntrustedFunction = "Native function is outside the fixture allowlist.";
    internal const string LoadVeto = "Native fixture load vetoed publication.";
    internal const string MissingDispatch = "Native function is outside the fixture dispatch table.";
}
