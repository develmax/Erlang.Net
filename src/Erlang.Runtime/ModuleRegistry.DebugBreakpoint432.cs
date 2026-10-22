namespace Erlang;

public sealed partial class ModuleRegistry
{
    private DebugBreakpointImage432[] debugBreakpoints432 = [];

    internal async ValueTask<Term> SetDebugBreakpoint432(ProcessContext context, DebugBreakpointPattern432 pattern, bool enable)
        => await ModifyCodeAsync(
            context,
            () =>
        {
            lock (gate)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                ThrowIfSourceClosing();
                var staged = new List<DebugBreakpointImage432>();
                var changes = new Dictionary<ModuleCodeImage, HashSet<int>>(ReferenceEqualityComparer.Instance);
                foreach (var image in codeImages.Values)
                {
                    if (image.ExecutionOwner is not PreparedBeamImage prepared) continue;
                    var matches = prepared.Code.Functions.Where(function => pattern.Matches(function)
                        && !prepared.ExecutionIdentity.NativePrimitives.ContainsKey((function.Name, function.Arity))).ToArray();
                    if (matches.Length > 0) changes.Add(image, matches.Select(function => function.EntryLabel).ToHashSet());
                }
                int matched = changes.Values.Sum(entries => entries.Count);
                foreach (var row in Volatile.Read(ref debugBreakpoints432))
                {
                    if (!row.Image.TryGetTarget(out var image)) continue;
                    if (!changes.TryGetValue(image, out var selected))
                    {
                        staged.Add(row);
                        continue;
                    }
                    var entries = row.Entries.ToHashSet();
                    if (enable) entries.UnionWith(selected); else entries.ExceptWith(selected);
                    if (entries.Count > 0) staged.Add(new(row.Image, entries));
                    changes.Remove(image);
                }
                if (enable) foreach (var change in changes) staged.Add(new(new(change.Key), change.Value));
                DebugBreakpointImage432[] publication = staged.ToArray();
                context.Cancellation.ThrowIfCancellationRequested();
                // Immutable prepared entries become active in one real publication.
                // Managed readers acquire the complete snapshot; no native code is patched.
                Volatile.Write(ref debugBreakpoints432, publication);

                return Term.I(matched);
            }
        }
        );

    internal bool HasDebugBreakpoint432(ModuleCodeImage image, int entry)
    {
        foreach (var row in Volatile.Read(ref debugBreakpoints432))
            if (row.Image.TryGetTarget(out var target) && ReferenceEquals(target, image)) return row.Entries.Contains(entry);

        return false;
    }
}
