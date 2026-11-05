internal static class NifPlatformValidationDiagnostics
{
    internal const string Expected32 = "The 32-bit runtime gate probe requires an actual pointer-size-4 process.";
    internal const string UnexpectedRuntime = "The 32-bit runtime source profile changed; require new full runtime/NIF acceptance.";
    internal const string VariadicProbe = "The native variadic probe did not preserve managed argument values or widths.";
}
