using System.Reflection;
using System.Runtime.Loader;

namespace Erlang.NifNativeBackend;

internal static class NifNativeProviderContextChecks
{
    internal static void Run()
    {
        var context = new AssemblyLoadContext(NifNativeFenceNames.ProviderContext, isCollectible: true);
        try
        {
            Assembly copy = context.LoadFromAssemblyPath(typeof(ProcessRuntime).Assembly.Location);
            Type gate = copy.GetType(NifNativeFenceNames.GateType)!;
            Type platform = copy.GetType(NifNativeFenceNames.PlatformType)!;
            Refused(gate, NifNativeFenceNames.VerifyProvider, []);
            Refused(gate, NifNativeFenceNames.Enter, [CancellationToken.None]);
            Refused(platform, NifNativeFenceNames.VerifyPlatform, []);
        }
        finally
        {
            context.Unload();
        }
    }

    private static void Refused(Type type, string method, object?[] arguments)
    {
        try
        {
            type.GetMethod(method, NifNativeFenceNames.StaticMembers)!.Invoke(null, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is NotSupportedException error
            && error.Message == NifNativeLifecycleNames.ProviderContext)
        {
            return;
        }
        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
    }
}
