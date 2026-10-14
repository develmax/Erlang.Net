using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    private readonly object gate = new();
    private readonly ModuleRegistry modules;
    private readonly string node;
    private readonly Dictionary<ReferenceTerm, WeakReference<ReferenceResourceEntry>> entries = new();
    private bool closed;

    internal RuntimeReferenceResources(ModuleRegistry modules, string node)
    {
        this.modules = modules;
        this.node = node;
    }

    public ResourceReferenceTerm CreatePrepared(PreparedBeamImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!ReferenceEquals(image.Owner, modules))
            throw new ArgumentException(ReferenceResourceNames.ForeignPrepared, nameof(image));
        if (image.IsConsumed)
            throw new InvalidOperationException(ReferenceResourceNames.ConsumedPrepared);

        return Create(ReferenceResourceKind.PreparedBeam, image);
    }

    public ResourceReferenceTerm CreateBinary(ReadOnlyMemory<byte> bytes)
        => Create(ReferenceResourceKind.BinarySnapshot, new BitString(bytes.Span));

    public ReferenceResourceLease<PreparedBeamImage>? AcquirePrepared(ReferenceTerm reference)
        => Acquire<PreparedBeamImage>(reference, ReferenceResourceKind.PreparedBeam);

    internal bool TryReadPreparedMetadata(ReferenceTerm reference, out PreparedBeamImage? image)
    {
        lock (gate)
            return TryReadPreparedMetadataLocked(reference, out image);
    }

    internal Term FinishPreparedReferences(IReadOnlyList<ReferenceTerm> references)
        => modules.FinishLoaderReferences(this, references);

    internal Term AdmitPreparedReferences(IReadOnlyList<ReferenceTerm> references)
    {
        lock (gate)
        {
            var images = new List<PreparedBeamImage>();
            foreach (ReferenceTerm reference in references)
            {
                if (!TryReadPreparedMetadataLocked(reference, out PreparedBeamImage? image))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                images.Add(image!);
            }

            return modules.FinishLoaderImages(images.ToArray());
        }
    }

    private bool TryReadPreparedMetadataLocked(ReferenceTerm reference, out PreparedBeamImage? image)
    {
        image = null;
        if (closed || reference is not ResourceReferenceTerm resource ||
            !ReferenceEquals(resource.Entry.Owner, this) || resource.ResourceKind != ReferenceResourceKind.PreparedBeam)
            return false;

        return resource.Entry.TryReadPreparedMetadata(out image);
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
        ArgumentNullException.ThrowIfNull(reference);
        lock (gate)
        {
            if (!closed && reference.Node == node && reference.Creation == ReferenceResourceParameters.LocalCreation &&
                reference.Words.Count == ReferenceIdentityLayout.LocalWords && (reference.Words[1] & ReferenceNumberLayout.MagicMarker) != 0 &&
                entries.TryGetValue(reference, out var weak) && weak.TryGetTarget(out var entry) && entry.Published && entry.IsAlive)
                return new ResourceReferenceTerm(entry);

            return new ReferenceTerm(reference.Node, reference.Words, reference.Creation);
        }
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
                if (weak.TryGetTarget(out var entry))
                    entry.Close();
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
                    node,
                    [
                        BinaryPrimitives.ReadUInt32LittleEndian(bytes) & ReferenceIdentityLayout.LocalFirstWordMask,
                        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizeof(uint))) & ReferenceNumberLayout.CountMiddleMask | ReferenceNumberLayout.MagicMarker,
                        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(2 * sizeof(uint)))
                    ],
                    ReferenceResourceParameters.LocalCreation
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
