namespace Erlang;

internal sealed partial class NativeMapMaterializedLayout505
{
    // Trusted registered views only. Ordinary portable materialization keeps its
    // existing defensive-copy constructor and never enters this overload.
    internal NativeMapMaterializedLayout505(
        ProcessRuntime runtime,
        object domain,
        OwnedMapPayload517 payload,
        HamtViewNode? root,
        NifNativeMapHashRetention504? retention
    )
    {
        owner = new(runtime);
        Domain = domain;
        PhysicalEntries = payload.Physical;
        Root = root;
        HashRetention = retention;
    }
}
