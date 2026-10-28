namespace Erlang;

internal sealed class NativeResourceLoadTransaction527 : IDisposable
{
    private readonly NativeResourceTypeTable527 table;
    private readonly NativeResourceDomain524 domain;
    private readonly NifNativeResourceImage524 image;
    private readonly NifResourceImagePin524 authority;
    private readonly List<NativeResourceTypeOpen527> opens = new();
    private bool loading = true, completed;
    private Exception?[]? publicationFailures527;
    internal NativeResourceTypeTable527 Table527 => table;
    internal bool Loading527 => loading && !completed;

    internal NativeResourceLoadTransaction527(
        NativeResourceTypeTable527 table,
        NativeResourceDomain524 domain,
        NifNativeResourceImage524 image,
        NifResourceImagePin524 authority
    )
    {
        this.table = table;
        this.domain = domain;
        this.image = image;
        this.authority = authority;
    }

    internal NativeResourceType524? Open527(
        string name,
        NativeResourceCallbacks527 callbacks,
        int flags,
        out int tried
    )
    {
        tried = flags;
        if (!loading || completed) return null;
        var version = new NativeResourceCallbackVersion527(image, callbacks);
        NativeResourceType524? type = table.FindPending527(image.Image.Module, name);
        bool created = type is null;
        if (created ? (flags & (int)NativeResourceOpenFlags527.Create) == 0
            : (flags & (int)NativeResourceOpenFlags527.Takeover) == 0)
        {
            type?.ReleasePending527();
            version.Dispose();

            return null;
        }
        type ??= domain.CreatePendingType527(name, image);
        try
        {
            if (created) type!.RetainPending527();
            try
            {
                var node = created ? new LinkedListNode<NativeResourceType524>(type) : null;
                opens.Add(new(
                    type,
                    version,
                    created,
                    node
                ));
            }
            catch
            {
                type.ReleasePending527();
                throw;
            }
        }
        catch
        {
            if (created) domain.DiscardPendingType527(type!);
            version.Dispose();
            throw;
        }
        tried = (int)(created ? NativeResourceOpenFlags527.Create : NativeResourceOpenFlags527.Takeover);

        return type;
    }

    internal void EndLoading527() => loading = false;

    internal NativeResourcePublication527 PreparePublication527()
    {
        if (completed || loading) throw new InvalidOperationException(NativeResource527Names.LoadPhase);
        NativeResourceTypeOpen527[] snapshot = opens.ToArray();
        publicationFailures527 = new Exception?[checked(snapshot.Length * 2 + 2)];
        foreach (NativeResourceTypeOpen527 opened in snapshot) opened.Version527.Prepare527(authority);
        var publication = new NativeResourcePublication527(this, table, snapshot);
        table.EnterPublication529();
        try
        {
            table.ValidatePublication527();
            publication.Admit527();
        }
        catch
        {
            table.LeavePublication529();
            throw;
        }

        return publication;
    }

    internal void FinishPublication527(NativeResourceTypeOpen527[] snapshot)
    {
        // Ready is already published. Never throw a post-commit cleanup failure
        // back into a caller that would unload this now-live native owner.
        // All failure slots were allocated before entering the writer/Ready gate.
        completed = true;
        Exception?[] failures = publicationFailures527!;
        int index = 0;
        foreach (NativeResourceTypeOpen527 opened in snapshot)
        {
            try
            {
                opened.Type527.ReleasePending527();
            }
            catch (Exception error)
            {
                failures[index] = error;
            }
            index++;
            try
            {
                opened.Previous527?.Dispose();
            }
            catch (Exception error)
            {
                failures[index] = error;
            }
            index++;
        }
        opens.Clear();
        try
        {
            authority.Dispose();
        }
        catch (Exception error)
        {
            failures[index] = error;
        }
        index++;
        try
        {
            domain.EndTypeLoad527();
        }
        catch (Exception error)
        {
            failures[index] = error;
        }
        image.Image.NifBindings.RecordResourcePublicationFailures527(failures);
    }

    public void Dispose()
    {
        if (completed) return;
        completed = true;
        loading = false;
        // Failure fields are part of already-admitted operation records. Always
        // attempt every independent retirement even if one fails.
        foreach (NativeResourceTypeOpen527 opened in opens)
        {
            try
            {
                opened.Version527.Dispose();
            }
            catch (Exception error)
            {
                opened.RollbackVersionFailure527 = error;
            }
            try
            {
                opened.Type527.ReleasePending527();
            }
            catch (Exception error)
            {
                opened.RollbackPendingFailure527 = error;
            }
            if (opened.Created527)
                try
                {
                    domain.DiscardPendingType527(opened.Type527);
                }
                catch (Exception error)
                {
                    opened.RollbackTypeFailure527 = error;
                }
        }
        Exception? authorityFailure = null, domainFailure = null;
        try
        {
            authority.Dispose();
        }
        catch (Exception error)
        {
            authorityFailure = error;
        }
        try
        {
            domain.EndTypeLoad527();
        }
        catch (Exception error)
        {
            domainFailure = error;
        }
        var failures = new List<Exception>();
        foreach (NativeResourceTypeOpen527 opened in opens)
        {
            if (opened.RollbackVersionFailure527 is { } version) failures.Add(version);
            if (opened.RollbackPendingFailure527 is { } pending) failures.Add(pending);
            if (opened.RollbackTypeFailure527 is { } type) failures.Add(type);
        }
        opens.Clear();
        if (authorityFailure is not null) failures.Add(authorityFailure);
        if (domainFailure is not null) failures.Add(domainFailure);
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
