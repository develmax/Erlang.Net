using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    private readonly object gate = new();
    private readonly ModuleRegistry modules;
    private readonly LocalNodeIdentity localIdentity;
    private readonly Dictionary<ReferenceTerm, WeakReference<ReferenceResourceEntry>> entries = new();
    private bool closed;

    internal RuntimeReferenceResources(ModuleRegistry modules, LocalNodeIdentity localIdentity)
    {
        this.modules = modules;
        this.localIdentity = localIdentity;
    }

    public ResourceReferenceTerm CreatePrepared(PreparedBeamImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!ReferenceEquals(image.Owner, modules))
            throw new ArgumentException(ReferenceResourceNames.ForeignPrepared, nameof(image));
        if (image.IsConsumed)
            throw new InvalidOperationException(ReferenceResourceNames.ConsumedPrepared);

        return CreatePreparedNative807(image);
    }

    public ResourceReferenceTerm CreateBinary(ReadOnlyMemory<byte> bytes)
        => Create(ReferenceResourceKind.BinarySnapshot, new BitString(bytes.Span));

    public ReferenceResourceLease<PreparedBeamImage>? AcquirePrepared(ReferenceTerm reference)
        => AcquirePreparedNative807(reference);

    internal Term FinishPreparedReferences(IReadOnlyList<ReferenceTerm> references)
    {
        lock (gate)
            if (closed && references.Count != 0)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

        return modules.FinishLoaderReferences(this, references);
    }

    internal void RequirePreparedFinishOpen807(int count)
    {
        lock (gate)
            if (closed && count != 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    public ReferenceResourceLease<BitString>? AcquireBinary(ReferenceTerm reference)
        => Acquire<BitString>(reference, ReferenceResourceKind.BinarySnapshot);

    public int LiveResources
    {
        get
        {
            lock (gate)
            {
                RemoveDead();

                return entries.Count;
            }
        }
    }

    public ReferenceTerm Rehydrate(ReferenceTerm reference)
    {
        return Rehydrate(reference, localIdentity.Snapshot);
    }

    internal ReferenceTerm Rehydrate(ReferenceTerm reference, LocalNodeSnapshot wire)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ResourceReferenceTerm? selected = null;
        IDisposable? producer = null;
        ReferenceTerm restored;
        lock (gate)
        {
            bool local = ReferenceEquals(reference.LocalIdentity, localIdentity) || reference.Node == wire.Node && reference.Creation == wire.Creation;
            restored = local
                ? ReferenceEquals(reference.LocalIdentity, localIdentity) && (reference.IsLocalPid775 || reference.IsOrdinaryLocal562)
                    ? new ReferenceTerm(reference)
                    : ReferenceTerm.RestoreLocal775(localIdentity, reference.Words)
                : new ReferenceTerm(reference.Node, reference.Words, reference.Creation);
            if (!closed && local && reference.Words.Count == ReferenceIdentityLayout.LocalWords
                && (reference.Words[1] & ReferenceNumberLayout.MagicMarker) != 0
                && entries.TryGetValue(restored, out var weak) && weak.TryGetTarget(out var entry) && entry.Published && entry.IsAlive)
            {
                try
                {
                    if (entry.Kind == ReferenceResourceKind.PreparedBeam)
                        producer = entry.Read<PreparedLoaderOwner807>().Retain807(PreparedLoaderLeaseKind807.Operation);
                    else if (entry.Kind == ReferenceResourceKind.Atomics413)
                        producer = entry.Read<AtomicsGuestRoot413>().Array.RetainBacking586(AtomicsLeaseKind586.Producer);
                    else if (entry.Kind == ReferenceResourceKind.Counters421)
                        producer = entry.Read<CountersGuestRoot421>().Array.RetainBacking596(CountersLeaseKind596.Producer);
                    else if (entry.Kind == ReferenceResourceKind.Regex411)
                        producer = entry.Read<RePattern411>().Backing597.Retain(ReMagicLeaseKind597.Guest);
                    else if (entry.Kind == ReferenceResourceKind.BinarySearchPattern)
                        producer = entry.Read<BinarySearchPattern>().Backing598.Retain598(BinaryProgramLeaseKind598.Guest);
                    else
                        return new ResourceReferenceTerm(entry);
                }
                catch (ObjectDisposedException)
                {
                    return restored;
                }
                try
                {
                    selected = new ResourceReferenceTerm(entry);
                }
                catch
                {
                    producer!.Dispose();
                    throw;
                }
            }
        }
        if (selected is null)
            return restored;
        // No registry gate is held across guest-heap publication or host acquisition.
        using (producer)
            return selected.PreparedBacking807 is { } prepared
                ? OwnedTermInvocation506.AdmitRehydratedPrepared807(selected, prepared)
                : selected.BinaryBacking598 is { } program
                ? OwnedTermInvocation506.AdmitRehydratedBinary598(selected, program)
                : selected.RegexBacking597 is { } regex
                ? OwnedTermInvocation506.AdmitRehydratedRegex597(selected, regex)
                : selected.CountersBacking596 is { } counters
                ? OwnedTermInvocation506.AdmitRehydratedCounters596(selected, counters)
                : OwnedTermInvocation506.AdmitRehydratedAtomics586(selected, selected.AtomicsBacking586!);
    }

    internal void Save(ReferenceResourceEntry entry)
    {
        lock (gate)
            if (!closed && ReferenceEquals(entry.Owner, this) && entry.IsAlive)
                entry.Published = true;
    }

    internal void Close()
    {
        lock (gate)
        {
            closed = true;
            foreach (var weak in entries.Values)
                if (weak.TryGetTarget(out var entry) && entry.Kind is not (ReferenceResourceKind.NativeResource524 or ReferenceResourceKind.Regex411 or ReferenceResourceKind.PreparedBeam))
                    entry.Close();
            // Native entries stay readable by already-admitted guest roots until
            // real physical resource retirement. The strong identity index is
            // removed by ForgetNativeResource524; closed admission cannot add one.
            entries.Clear();
        }
    }

    private ResourceReferenceTerm Create(ReferenceResourceKind kind, object payload)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, ReferenceResourceNames.Registry);
            RemoveDead();
            ReferenceTerm identity;
            do
            {
                byte[] bytes = RandomNumberGenerator.GetBytes(ReferenceResourceParameters.IdentityBytes);
                identity = new ReferenceTerm(
                    localIdentity,
                    [
                        BinaryPrimitives.ReadUInt32LittleEndian(bytes) & ReferenceIdentityLayout.LocalFirstWordMask,
                        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizeof(uint))) & ReferenceNumberLayout.CountMiddleMask | ReferenceNumberLayout.MagicMarker,
                        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(2 * sizeof(uint)))
                    ]
                );
            }
            while (entries.ContainsKey(identity));
            var entry = new ReferenceResourceEntry(
                this,
                identity,
                kind,
                payload
            );
            entries.Add(identity, new(entry));

            return new ResourceReferenceTerm(entry);
        }
    }

    private ReferenceResourceLease<T>? Acquire<T>(ReferenceTerm reference, ReferenceResourceKind kind) where T : class
    {
        ReferenceResourceEntry? entry;
        lock (gate)
        {
            if (closed || reference is not ResourceReferenceTerm resource || !ReferenceEquals(resource.Entry.Owner, this) || resource.ResourceKind != kind || !resource.Entry.IsAlive)
                return null;
            entry = resource.Entry;
        }
        try
        {
            _ = entry.Read<T>();
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
        catch (InvalidOperationException) when (kind == ReferenceResourceKind.PreparedBeam)
        {
            return null;
        }
        lock (gate)
        {
            if (closed || !entry.IsAlive)
                return null;

            return new(entry);
        }
    }

    private void RemoveDead()
    {
        foreach (var pair in entries.ToArray())
            if (!pair.Value.TryGetTarget(out var entry) || !entry.IsAlive)
                entries.Remove(pair.Key);
    }
}
