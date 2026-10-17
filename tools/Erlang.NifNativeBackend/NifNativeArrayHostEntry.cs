namespace Erlang.NifNativeBackend;

public static class NifNativeArrayHostEntry
{
    public static Task Empty() => NifNativeArrayHostChecks.Run(NifNativeArrayHostChecks.Empty);

    public static Task Order() => NifNativeArrayHostChecks.Run(NifNativeArrayHostChecks.Order);

    public static Task Limits() => NifNativeArrayHostChecks.Run(NifNativeArrayHostChecks.Limits);

    public static Task Containment() => NifNativeArrayHostChecks.Run(NifNativeArrayHostChecks.Containment);
}
