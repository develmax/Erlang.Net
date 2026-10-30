namespace Erlang;

internal static class NifNativeAdmissionReason544
{
    internal static string Format(
        NifImageAdmissionFailure failure,
        ProcessRuntime runtime,
        string module
    ) => failure switch
    {
        NifImageAdmissionFailure.Reload => NifNativeLoadReasonText.Reload544,
        NifImageAdmissionFailure.UpgradeUnsupported => NifNativeLoadReasonText.UpgradeUnsupported544,
        NifImageAdmissionFailure.OldCode => NifNativeLoadReasonText.OldCodePrefix544
            + NifNativeLoadReason.DisplayModule544(runtime, module) + NifNativeLoadReasonText.OldCodeSuffix544,
        _ => throw new ArgumentOutOfRangeException(nameof(failure))
    };
}
