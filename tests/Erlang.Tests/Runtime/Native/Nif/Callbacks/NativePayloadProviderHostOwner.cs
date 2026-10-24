using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal sealed class NativePayloadProviderHostOwner : IDisposable
{
    private readonly object actual;
    internal NifReallocationHostOwner Payload
    {
        get;
    }
    internal object Domain
    {
        get;
    }

    internal NativePayloadProviderHostOwner(ProcessRuntime runtime)
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(NativePayloadProviderHostNames.OwnerType, true)!;
        actual = Activator.CreateInstance(type, nonPublic: true)!;
        Domain = NifReallocationHostOwner.RuntimeDomain(runtime);
        type.GetMethod(NativePayloadProviderHostNames.Join, NativePayloadProviderHostNames.Members)!.Invoke(actual, [Domain]);
        Payload = new NifReallocationHostOwner(actual);
    }

    public void Dispose()
    {
        Payload.Reclaim();
        if (!(bool)actual.GetType().GetMethod(NativePayloadProviderHostNames.End, NativePayloadProviderHostNames.Members)!.Invoke(actual, null)!)
            throw new InvalidOperationException(NativePayloadProviderHostNames.Failure);
    }
}
