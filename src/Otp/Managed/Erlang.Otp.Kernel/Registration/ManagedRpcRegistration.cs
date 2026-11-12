namespace Erlang;

public sealed class ManagedRpcRegistration : IRuntimeManagedLibrary456
{
    public void Register(ModuleRegistry registry, RuntimeManagedLibraryStage456 stage)
    {
        if (stage != RuntimeManagedLibraryStage456.Rpc)
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }

        Otp.RpcLibrary.Register(registry);
    }
}
