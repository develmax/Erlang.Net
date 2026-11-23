namespace Erlang.Compiler;

internal static class BeamFrameValueLiveness
{
    internal readonly record struct Operation(
        IReadOnlyList<int> Uses,
        int? Definition,
        bool ClobbersXRegisters
    );

    // The operation stream uses distinct source value identities. The result of a call
    // is defined after the call invalidates the prior X-register values.
    internal static HashSet<int> FindSavedValues(
        IEnumerable<int> definitionsAtEntry,
        IEnumerable<Operation> operations
    )
    {
        var available = definitionsAtEntry.ToHashSet();
        var clobbered = new HashSet<int>();
        var saved = new HashSet<int>();
        foreach (Operation operation in operations)
        {
            foreach (int use in operation.Uses)
            {
                if (clobbered.Contains(use))
                {
                    saved.Add(use);
                }
            }

            if (operation.ClobbersXRegisters)
            {
                clobbered.UnionWith(available);
                available.Clear();
            }

            if (operation.Definition is int definition)
            {
                available.Add(definition);
            }
        }

        return saved;
    }
}
