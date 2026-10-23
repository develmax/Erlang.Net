namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Signature strings and two integers only: no source image, function,
    // environment, process, or delegate. Native fun entries outlive code purge.
    private readonly Dictionary<(string Module, int Index, int Arity, string Unique), RuntimeFunLegacyFields> funLegacyEntries = new();

    internal LocalFunctionIdentity CanonicalFunctionIdentity(LocalFunctionIdentity identity, int arity)
    {
        lock (gate)
        {
            var key = (identity.Module.Name, identity.Index, arity, Convert.ToHexString(identity.Unique.ToArray()));
            if (!funLegacyEntries.TryGetValue(key, out RuntimeFunLegacyFields first))
            {
                first = new(identity.OldUnique, identity.OldIndex);
                funLegacyEntries.Add(key, first);
            }

            return identity.OldUnique == first.OldUnique && identity.OldIndex == first.OldIndex
                ? identity
                : identity with { OldUnique = first.OldUnique, LegacyIndex = first.OldIndex };
        }
    }

    private void RecordSourceFunEntries(ISourceLocalFunctionResolver source)
    {
        foreach (var entry in source.LocalFunctionIdentities())
            _ = CanonicalFunctionIdentity(entry.Identity, entry.Arity);
    }

    private void RecordPreparedFunEntries(PreparedBeamImage image)
        => RecordSourceFunEntries(image);
}
