namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal bool HasBootstrapFileSystem
    {
        get
        {
            lock (gate)
            {
                return IsBootstrapBinding(RuntimeBootstrapFileNames.SpawnSystemProcess, 3, RuntimeBootstrapFileSystem.SpawnBody)
                    && IsBootstrapBinding(RuntimeBootstrapFileNames.IsSystemProcess, 1, RuntimeBootstrapFileSystem.IsSystemBody);
            }
        }
    }

    private bool IsBootstrapBinding(string function, int arity, ErlangFunction body)
    {
        var key = (RuntimeBootstrapFileNames.InternalModule, function, arity);

        return functions.TryGetValue(key, out ErlangFunction? actual) && ReferenceEquals(actual, body)
            && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, function, arity);
    }

    internal void InstallBootstrapFileSystem()
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            if (HasBootstrapFileSystem)
                return;
            foreach (var signature in new[]
            {
                (RuntimeBootstrapFileNames.SpawnSystemProcess, 3),
                (RuntimeBootstrapFileNames.IsSystemProcess, 1)
            })
                if (functions.ContainsKey((RuntimeBootstrapFileNames.InternalModule, signature.Item1, signature.Item2)))
                    throw new InvalidOperationException(RuntimeBootstrapFileNames.Conflict);
            InstallPreloaded(target =>
            {
                target.Register(
                    RuntimeBootstrapFileNames.InternalModule,
                    RuntimeBootstrapFileNames.SpawnSystemProcess,
                    3,
                    RuntimeBootstrapFileSystem.SpawnBody
                );
                target.Register(
                    RuntimeBootstrapFileNames.InternalModule,
                    RuntimeBootstrapFileNames.IsSystemProcess,
                    1,
                    RuntimeBootstrapFileSystem.IsSystemBody
                );
            });
        });
    }
}
