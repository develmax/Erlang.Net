namespace Erlang;

public sealed partial class ModuleRegistry
{
    private List<string>? nifTaints;
    private HashSet<string>? nifTaintSet;

    // Metadata only: called while the reservation already owns code-modification permission.
    internal void AddNifTaint(string module)
    {
        lock (gate)
        {
            nifTaints ??= new();
            nifTaintSet ??= new(StringComparer.Ordinal);
            if (nifTaintSet.Add(module))
                nifTaints.Add(module);
        }
    }

    internal string[] GetNifTaints()
    {
        lock (gate)
        {
            return nifTaints?.ToArray() ?? [];
        }
    }
}
