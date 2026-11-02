using System.Runtime.ExceptionServices;

namespace Erlang;

// Only the two reviewed immutable runtime constant families can enter this pool.
// Every value lives in real sealed native literal storage owned by this VM.
internal sealed class VmConstantPool357(ProcessRuntime runtime)
{
    private readonly object gate = new();
    private readonly Dictionary<string, VmConstantEntry357> atoms = new(StringComparer.Ordinal);
    private VmConstantEntry357? errorInfo;
    private bool closed;

    internal Term AtomBinary(ProcessContext context, Atom atom, BitString template)
        => Read(context, atom.Name, template);

    internal MapTerm ErrorInfo(ProcessContext context)
        => (MapTerm)Read(context, null, ErlangErrorHelperMetadata.Info);

    internal ProcessRuntime Runtime357 => runtime;

    private Term Read(ProcessContext context, string? atomName, Term template)
    {
        VmConstantEntry357? entry = null;
        VmConstantEntry357? candidate = null;
        ModuleLiteralRoots801? roots = null;
        Term? result = null;
        var failures = new List<Exception>(3);
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);
                entry = atomName is null ? errorInfo : atoms.GetValueOrDefault(atomName);
                roots = entry?.Roots.Retain801();
            }
            if (entry is null)
            {
                // Native construction/registration never runs under the pool gate.
                candidate = VmConstantEntry357.Create(this, template);
                lock (gate)
                {
                    ObjectDisposedException.ThrowIf(closed, this);
                    entry = atomName is null ? errorInfo : atoms.GetValueOrDefault(atomName);
                    if (entry is null)
                    {
                        // Transfer ownership only AFTER the potentially failing add.
                        if (atomName is null) errorInfo = candidate;
                        else atoms.Add(atomName, candidate);
                        entry = candidate;
                        candidate = null;
                    }
                    roots = entry.Roots.Retain801();
                }
            }
            OwnedTermInvocation506 invocation = context.CurrentOwned506!;
            invocation.Frame.SetPreparedWithLiteral801(invocation.ResultSlot507, entry.Value, roots);
            result = invocation.Frame.Read(invocation.ResultSlot507);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // Every admitted temporary holder is released even if a losing
            // candidate's independent cleanup fails. Preserve the primary error.
            try
            {
                roots?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                candidate?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);

        return result!;
    }

    internal Task[] Close()
    {
        VmConstantEntry357[] entries;
        Exception?[] failures;
        lock (gate)
        {
            if (closed) return [];
            // Allocate the complete snapshot/cleanup buffer BEFORE committing
            // closure. A failed snapshot leaves the owning pool retryable.
            entries = atoms.Values.ToArray();
            if (errorInfo is { } info) entries = [.. entries, info];
            failures = new Exception?[entries.Length];
            closed = true;
            atoms.Clear();
            errorInfo = null;
        }
        for (int index = 0; index < entries.Length; index++)
            try
            {
                entries[index].Dispose();
            }
            catch (Exception error)
            {
                failures[index] = error;
            }

        // Diagnostic allocation occurs only after every real owner was released.
        return failures.Where(error => error is not null).Select(error => Task.FromException(error!)).ToArray();
    }
}
