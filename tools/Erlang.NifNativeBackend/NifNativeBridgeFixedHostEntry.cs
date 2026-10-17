namespace Erlang.NifNativeBackend;

public static class NifNativeBridgeFixedHostEntry
{
    public static Task Layout() => NifNativeTypeHostChecks.Run(NifNativeBridgeFixedHostChecks.Layout);

    public static Task LongWidth() => NifNativeTypeHostChecks.Run(NifNativeBridgeFixedHostChecks.LongWidth);

    public static Task CompoundTerms() => NifNativeTypeHostChecks.Run(NifNativeBridgeFixedHostChecks.CompoundTerms);
}
