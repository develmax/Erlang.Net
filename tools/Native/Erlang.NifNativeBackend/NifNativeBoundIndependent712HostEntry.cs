namespace Erlang.NifNativeBackend;

public static class NifNativeBoundIndependent712HostEntry
{
    public static Task Run() => NifNativeBinaryHostChecks.Run(NifNativeBoundIndependent712HostChecks.Run);
}
