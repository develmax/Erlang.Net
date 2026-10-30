namespace Erlang;

internal sealed partial class NativeResourceLoadTransaction527 : IDisposable
{
    private readonly NativeResourceTypeTable527 table;
    private readonly NativeResourceDomain524 domain;
    private readonly NifNativeResourceImage524 image;
    private readonly NifResourceImagePin524 authority;
    private readonly List<NativeResourceTypeOpen527> opens = new();
    private bool loading = true, completed;
    private readonly NativeResourcePublication527 publication527;
    private Exception?[] publicationFailures527;
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
        publicationFailures527 = new Exception?[NativeResourcePublication527Parameters.FinalFailures];
        publication527 = new(this, table, opens);
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
        NativeResourceUpgradeSnapshot544? admittedUpgrade = upgradeSnapshot544;
        NativeResourceType524? type = admittedUpgrade is null
            ? table.FindPending527(image.Image.Module, name) : admittedUpgrade.FindPending544(name);
        bool created = type is null;
        if (created ? (flags & (int)NativeResourceOpenFlags527.Create) == 0
            : (flags & (int)NativeResourceOpenFlags527.Takeover) == 0)
        {
            type?.ReleasePending527();
            version.Dispose();

            return null;
        }
        bool pending = !created;
        bool createdType = false;
        try
        {
            if (created)
            {
                type = admittedUpgrade is null ? domain.CreatePendingType527(name, image)
                    : domain.CreateUpgradePendingType544(
                        name,
                        image,
                        this,
                        admittedUpgrade
                    );
                createdType = true;
                type.RetainPending527();
                pending = true;
            }
            var node = created ? new LinkedListNode<NativeResourceType524>(type!) : null;
            int nextCount = checked(opens.Count + 1);
            int failureIndex = checked(opens.Count * NativeResourcePublication527Parameters.FailuresPerOpen);
            var opened = new NativeResourceTypeOpen527(
                type!,
                version,
                created,
                node,
                failureIndex
            );
            opens.EnsureCapacity(nextCount);
            int failureCount = checked(nextCount * NativeResourcePublication527Parameters.FailuresPerOpen
                + NativeResourcePublication527Parameters.FinalFailures + UpgradeFailureCapacity544);
            if (publicationFailures527.Length < failureCount)
            {
                int capacity = publicationFailures527.Length;
                while (capacity < failureCount)
                    capacity = capacity <= Array.MaxLength / NativeResourcePublication527Parameters.CapacityGrowthFactor
                        ? capacity * NativeResourcePublication527Parameters.CapacityGrowthFactor : failureCount;
                publicationFailures527 = new Exception?[capacity];
            }
            // Fork callback custody while the native open call can still fail.
            // This pin protects code lifetime; it grants no name/close admission.
            version.Prepare527(authority);
            opens.Add(opened);
        }
        catch (Exception failure)
        {
            Exception? versionFailure = null, pendingFailure = null, typeFailure = null;
            try
            {
                version.Dispose();
            }
            catch (Exception error)
            {
                versionFailure = error;
            }
            if (pending)
                try
                {
                    type!.ReleasePending527();
                }
                catch (Exception error)
                {
                    pendingFailure = error;
                }
            if (createdType)
                try
                {
                    domain.DiscardPendingType527(type!);
                }
                catch (Exception error)
                {
                    typeFailure = error;
                }
            if (versionFailure is not null || pendingFailure is not null || typeFailure is not null)
            {
                var failures = new List<Exception> { failure };
                if (versionFailure is not null) failures.Add(versionFailure);
                if (pendingFailure is not null) failures.Add(pendingFailure);
                if (typeFailure is not null) failures.Add(typeFailure);
                throw new AggregateException(failures);
            }
            throw;
        }
        tried = (int)(created ? NativeResourceOpenFlags527.Create : NativeResourceOpenFlags527.Takeover);

        return type;
    }

    internal void EndLoading527()
    {
        if (!loading) return;
        publication527.Seal527(opens.Count);
        loading = false;
    }

    internal NativeResourcePublication527 PreparePublication527()
    {
        if (completed || loading) throw new InvalidOperationException(NativeResource527Names.LoadPhase);
        table.EnterPublication529();
        try
        {
            table.ValidatePublication527();
            publication527.Admit527();
        }
        catch
        {
            table.LeavePublication529();
            throw;
        }

        return publication527;
    }

    internal void FinishPublication527(int count)
    {
        // Ready is already published. Never throw a post-commit cleanup failure
        // back into a caller that would unload this now-live native owner.
        // All failure slots and callback pins were prepared before Open527 returned.
        completed = true;
        Exception?[] failures = publicationFailures527;
        for (int openIndex = 0; openIndex < count; openIndex++)
        {
            NativeResourceTypeOpen527 opened = opens[openIndex];
            int index = opened.PublicationFailureIndex527;
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
        }
        upgradeSnapshot544?.Release544();
        if (upgradeSnapshot544 is { } snapshot)
            Array.Copy(
                snapshot.Failures544,
                0,
                failures,
                count * NativeResourcePublication527Parameters.FailuresPerOpen
                    + NativeResourcePublication527Parameters.FinalFailures,
                snapshot.Failures544.Length
            );
        opens.Clear();
        int finalIndex = count * NativeResourcePublication527Parameters.FailuresPerOpen;
        try
        {
            authority.Dispose();
        }
        catch (Exception error)
        {
            failures[finalIndex] = error;
        }
        finalIndex++;
        try
        {
            domain.EndTypeLoad527();
        }
        catch (Exception error)
        {
            failures[finalIndex] = error;
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
        upgradeSnapshot544?.Release544();
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
        if (upgradeSnapshot544 is { } snapshot)
            foreach (var error in snapshot.Failures544)
                if (error is not null) failures.Add(error);
        if (authorityFailure is not null) failures.Add(authorityFailure);
        if (domainFailure is not null) failures.Add(domainFailure);
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
