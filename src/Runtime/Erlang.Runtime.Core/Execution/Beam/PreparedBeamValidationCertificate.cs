using System.Collections.Concurrent;

namespace Erlang;

// Structural validation only. This grants no code-image, actor, API, native BIF,
// source/admission or callable capability. Those gates still run per invocation.
internal sealed class PreparedBeamValidationCertificate
{
    private readonly HashSet<BeamLinkedFunction> members;
    private readonly ConcurrentDictionary<BeamLinkedFunction, bool> successful = new(ReferenceEqualityComparer.Instance);

    internal PreparedBeamValidationCertificate(BeamLinkedCode code, IEnumerable<BeamLinkedFunction> validated)
    {
        members = new(code.Functions, ReferenceEqualityComparer.Instance);
        foreach (BeamLinkedFunction entry in validated)
            RecordSuccess(entry);
    }

    internal bool HasSuccess(BeamLinkedFunction entry) => successful.ContainsKey(entry);

    internal void RecordSuccess(BeamLinkedFunction entry)
    {
        if (members.Contains(entry)) successful.TryAdd(entry, true);
    }
}
