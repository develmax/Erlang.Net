namespace Erlang.NifNativeBackend;

// Permanent managed-only entry; no native permission or Runtime internals are made public.
public static class NifNativeDefaultRouteHostEntry
{
    public static Task Run() => NifNativeDefaultRouteChecks.Run();

    public static Task Filename()
    {
        NifNativeFilenameControls.Run();

        return Task.CompletedTask;
    }
}
