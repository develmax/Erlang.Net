namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    private readonly Dictionary<ReMagicBacking597, ReferenceResourceEntry> regexEntries597 = new(ReferenceEqualityComparer.Instance);

    internal ResourceReferenceTerm CreateRegex411(RePattern411 pattern)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, ReMagicNames597.Closed);
            if (regexEntries597.TryGetValue(pattern.Backing597, out var previous)) return new ResourceReferenceTerm(previous);
            regexEntries597.EnsureCapacity(checked(regexEntries597.Count + 1));
            var reference = Create(ReferenceResourceKind.Regex411, pattern);
            try
            {
                pattern.Backing597.BindIdentity597(reference.Words);
                regexEntries597.Add(pattern.Backing597, reference.Entry);

                return reference;
            }
            catch
            {
                entries.Remove(reference.Entry.Identity);
                reference.Entry.Close();
                throw;
            }
        }
    }

    internal ReferenceResourceLease<RePattern411>? AcquireRegex411(ReferenceTerm reference)
        => Acquire<RePattern411>(reference, ReferenceResourceKind.Regex411);

    internal void ForgetRegex597(ReMagicBacking597 backing)
    {
        lock (gate)
        {
            if (!regexEntries597.Remove(backing, out var entry)) return;
            entries.Remove(entry.Identity);
            entry.Close();
        }
    }
}
