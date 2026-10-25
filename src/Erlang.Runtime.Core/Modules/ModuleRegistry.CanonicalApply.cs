namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal bool IsCanonicalModuleApply()
    {
        lock (gate)
        {
            var signature = (ErlangModuleNames.Module, ErlangBasicBifNames.Apply, ErlangApplyLayout.ModuleApplyArity);

            return !sourceOwners.ContainsKey(signature)
                && functions.TryGetValue(signature, out var body)
                && body == ErlangApplyDispatch.Invoke;
        }
    }
}
