namespace Erlang;

internal sealed class PreparedSourceLiteralView819 : IDisposable, IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ModuleRegistry registry;
    private readonly object templateOwner;
    private readonly ModuleLiteralDomain800 domain;
    private readonly IReadOnlyDictionary<Term, Term> values;
    private readonly HashSet<Pattern.Literal> patterns;
    private HashSet<NativeRecordDefinition>? nativeRecords323;
    private readonly Dictionary<object, Term> constants820 = new(ReferenceEqualityComparer.Instance);
    private readonly TaskCompletionSource publicationSettled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool disposed, attempted, consumed;
    private bool publication;
    private ModuleCodeImage? image;

    internal PreparedSourceLiteralView819(
        ModuleRegistry registry,
        object owner,
        ModuleLiteralDomain800 domain,
        IReadOnlyList<Term> templates,
        IReadOnlyList<Pattern.Literal> patterns
    )
    {
        this.registry = registry;
        templateOwner = owner;
        this.domain = domain;
        this.patterns = new(patterns, ReferenceEqualityComparer.Instance);
        var mapping = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < templates.Count; index++) mapping.Add(templates[index], domain.Literals800[index]);
        values = mapping;
    }

    internal void BindNativeRecords323(IReadOnlyList<NativeRecordDefinition> definitions)
    {
        lock (gate)
        {
            if (disposed || attempted || nativeRecords323 is not null)
                throw new InvalidOperationException(SourceLiteral819Names.Invalid);
            NativeRecordDefinition[] snapshot = definitions.ToArray();
            foreach (NativeRecordDefinition definition in snapshot)
                foreach (var field in definition.Fields)
                    if (field.Value is { } value && !values.ContainsKey(value))
                        throw new InvalidOperationException(SourceLiteral819Names.Invalid);
            nativeRecords323 = new(snapshot, ReferenceEqualityComparer.Instance);
        }
    }

    internal bool OwnsNativeRecordDefinition323(NativeRecordDefinition definition, ModuleCodeImage actualImage, ModuleLiteralLease800 lease)
    {
        lock (gate)
            return ReferenceEquals(image, actualImage) && domain.HasSourceLease819(lease)
                && nativeRecords323?.Contains(definition) == true;
    }

    internal void BindConstants820(IReadOnlyList<(object Node, Term Value)> slots)
    {
        lock (gate)
        {
            if (disposed || attempted || constants820.Count != 0)
                throw new InvalidOperationException(SourceLiteral819Names.Invalid);
            foreach (var slot in slots)
            {
                if (!values.ContainsKey(slot.Value)) throw new InvalidOperationException(SourceLiteral819Names.Invalid);
                constants820.Add(slot.Node, slot.Value);
            }
        }
    }

    internal bool TryReadConstant820(
        object node,
        ModuleCodeImage codeImage,
        ModuleLiteralLease800 lease,
        out Term value
    )
    {
        if (constants820.TryGetValue(node, out Term? template))
        {
            value = Read819(template, codeImage, lease);

            return true;
        }
        value = null!;

        return false;
    }

    internal void BeginPublication819(ModuleRegistry selected, object owner)
    {
        lock (gate)
        {
            if (!ReferenceEquals(selected, registry) || !ReferenceEquals(owner, templateOwner) || disposed || attempted)
                throw new InvalidOperationException(SourceLiteral819Names.Used);
            attempted = true;
            publication = true;
        }
    }

    internal void BindImage819(ModuleRegistry selected, ModuleCodeImage codeImage)
    {
        lock (gate)
        {
            if (!ReferenceEquals(selected, registry) || !publication || image is not null)
                throw new InvalidOperationException(SourceLiteral819Names.Invalid);
            registry.TrackSourceLiteralPublication819(this);
            domain.BindImage800(registry, codeImage);
            codeImage.BindSourceLiteralView819(this);
            image = codeImage;
        }
    }

    internal void EndPublication819(bool success)
    {
        bool close;
        lock (gate)
        {
            if (!publication) throw new InvalidOperationException(SourceLiteral819Names.Invalid);
            publication = false;
            consumed = success;
            close = !success;
            if (!success) disposed = true;
        }
        try
        {
            if (close) _ = domain.CloseCodeOwner800();
            publicationSettled.TrySetResult();
        }
        catch (Exception error)
        {
            publicationSettled.TrySetException(error);
            throw;
        }
    }

    internal ModuleCodeImage? Image819
    {
        get
        {
            lock (gate) return image;
        }
    }

    internal void RollbackPublication819()
    {
        lock (gate)
        {
            consumed = false;
            disposed = true;
        }
        _ = domain.CloseCodeOwner800();
    }

    internal bool ContainsPattern819(Pattern.Literal pattern) => patterns.Contains(pattern);

    internal Term Read819(Term template, ModuleCodeImage codeImage, ModuleLiteralLease800 lease)
    {
        if (!ReferenceEquals(image, codeImage) || !domain.HasSourceLease819(lease))
            throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        if (values.TryGetValue(template, out Term? value)) return value;
        // Syntax normalizers generate immediate tags and arities, not new
        // compound literal authority. Every compound must be in this pool.
        if (template is Atom or Nil || ProcessTermHeap506.IsImmediateInteger559(template)) return template;
        throw new NotSupportedException(SourceLiteral819Names.Unprepared);
    }

    public void Dispose()
    {
        bool close;
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            close = !consumed && !publication;
        }
        if (close) _ = domain.CloseCodeOwner800();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        Task? pending;
        lock (gate) pending = !publication ? null : publicationSettled.Task;
        if (pending is not null)
        {
            registry.RequireSourceLiteralAwait819();
            await pending.ConfigureAwait(false);
        }
        bool join;
        lock (gate) join = !consumed && !publication;
        if (join)
        {
            registry.RequireSourceLiteralAwait819();
            await domain.Retirement800.ConfigureAwait(false);
        }
    }
}
