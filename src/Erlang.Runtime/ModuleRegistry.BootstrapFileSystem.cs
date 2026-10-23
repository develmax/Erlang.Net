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

        if (!functions.TryGetValue(key, out ErlangFunction? actual))
            return false;
        if (ReferenceEquals(actual, body) && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, function, arity))
            return true;
        if (sourceOwners.GetValueOrDefault(key) is not PreparedBeamImage image
            || !ReferenceEquals(image.Owner, this)
            || !ReferenceEquals(codeImages.GetValueOrDefault(key.Item1), image.ExecutionIdentity.Image)
            || !image.ExecutionIdentity.NativePrimitives.TryGetValue((function, arity), out ErlangFunction? primitive)
            || !ReferenceEquals(primitive, body))
            return false;
        for (int index = 0; index < image.Code.Exports.Count; index++)
        {
            BeamLinkedFunction export = image.Code.Exports[index];
            if (export.Name == function && export.Arity == arity)
                return ReferenceEquals(image.Bodies[index], actual);
        }

        return false;
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
