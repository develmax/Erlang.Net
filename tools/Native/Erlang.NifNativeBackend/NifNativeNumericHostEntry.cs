namespace Erlang.NifNativeBackend;

public static class NifNativeNumericHostEntry
{
    public static Task IntegerGetters() => NifNativeNumericHostChecks.Run(NifNativeNumericHostChecks.IntegerGetters);

    public static Task IntegerBuilders() => NifNativeNumericHostChecks.Run(NifNativeNumericHostChecks.IntegerBuilders);

    public static Task Doubles() => NifNativeNumericHostChecks.Run(NifNativeNumericHostChecks.Doubles);

    public static Task EnvironmentAndContainment() => NifNativeNumericHostChecks.Run(NifNativeNumericHostChecks.EnvironmentAndContainment);
}
