namespace Erlang.NifNativeBackend;

public static class NifNativeLegacyStringHostEntry
{
    public static Task Latin1() => NifNativeLegacyStringHostChecks.Run(NifNativeLegacyStringHostChecks.Latin1);

    public static Task Utf8() => NifNativeLegacyStringHostChecks.Run(NifNativeLegacyStringHostChecks.Utf8);

    public static Task LongInputs() => NifNativeLegacyStringHostChecks.Run(NifNativeLegacyStringHostChecks.LongInputs);

    public static Task Termination() => NifNativeLegacyStringHostChecks.Run(NifNativeLegacyStringHostChecks.Termination);

    public static Task RejectedUtf8() => NifNativeLegacyStringHostChecks.Run(NifNativeLegacyStringHostChecks.RejectedUtf8);

    public static Task EnvironmentAndContainment() => NifNativeLegacyStringHostChecks.Run(NifNativeLegacyStringHostChecks.EnvironmentAndContainment);
}
