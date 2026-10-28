namespace Erlang;

internal sealed partial class NativeResourceTypeTable527(ProcessRuntime runtime, NativeResourceDomain524 domain)
{
    private readonly ReaderWriterLockSlim callbackGate = new(LockRecursionPolicy.SupportsRecursion);
    private readonly LinkedList<NativeResourceType524> committed = new();
    private bool closing;

    internal NativeResourceType524? Find527(string module, string name)
    {
        lock (namesGate529)
        {
            if (closing) return null;
            foreach (NativeResourceType524 type in committed)
                if (type.Module527 == module && type.Name == name) return type;

            return null;
        }
    }

    internal NativeResourceType524? FindPending527(string module, string name)
    {
        lock (namesGate529)
        {
            if (closing) return null;
            foreach (NativeResourceType524 type in committed)
                if (type.Module527 == module && type.Name == name)
                {
                    type.RetainPending527();

                    return type;
                }

            return null;
        }
    }

    internal NativeResourceLoadTransaction527 BeginLoad527(
        NifNativeResourceImage524 image, NifImageBindingReservation reservation)
    {
        if (!ReferenceEquals(image.Runtime, runtime)) throw new ArgumentException(NativeResource524Names.Foreign);

        domain.BeginTypeLoad527();
        NifResourceImagePin524? pin = null;
        try
        {
            pin = image.Image.NifBindings.RetainLoadingResource527(reservation);
            image.Image.NifBindings.BindResourceNames529(this, reservation);

            return new(
                this,
                domain,
                image,
                pin
            );
        }
        catch
        {
            pin?.Dispose();
            domain.EndTypeLoad527();
            throw;
        }
    }

    internal void ValidatePublication527()
    {
        if (closing) throw new ObjectDisposedException(NativeResource524Names.Closed);
    }

    internal void EnterRead527() => callbackGate.EnterReadLock();

    internal void LeaveRead527() => callbackGate.ExitReadLock();

    internal void EnterWrite527() => callbackGate.EnterWriteLock();

    internal void LeaveWrite527() => callbackGate.ExitWriteLock();

    // Caller owns the publication writer scope. All nodes/versions were allocated
    // before the actual image binding monitor is entered.
    internal void Commit527(NativeResourceTypeOpen527[] opens)
    {
        for (int index = opens.Length - 1; index >= 0; index--)
        {
            NativeResourceTypeOpen527 opened = opens[index];
            if (opened.Created527) committed.AddFirst(opened.Node527!);
            opened.Previous527 = opened.Type527.Install527(this, opened.Version527);
        }
    }

    internal void RetireImage527(ModuleCodeImage image)
    {
        NativeResourceImageNamesRetirement529 names;
        using (names = PrepareImageNamesRetirement529(image))
            names.Commit529();
        names.Finish529();
    }

    internal void TryRetireEmpty527(NativeResourceType524 type)
    {
        bool detached = false;
        callbackGate.EnterWriteLock();
        try
        {
            lock (namesGate529)
            {
                if (type.DetachIfEmpty527(out _))
                {
                    committed.Remove(type);
                    detached = true;
                }
            }
        }
        finally
        {
            callbackGate.ExitWriteLock();
        }
        // Detach revoked future payload/pending admission. Transfer the domain's
        // last ownership outside callback/name locks before unregistering the
        // opaque token and releasing its callback-version/image pin.
        if (detached)
            domain.RetireDetachedType575(type);
    }

    internal void BeginClose527()
    {
        NativeResourceType524[] candidates;
        callbackGate.EnterWriteLock();
        try
        {
            lock (namesGate529)
            {
                closing = true;
                candidates = committed.ToArray();
                foreach (NativeResourceType524 type in candidates) type.MarkImageRetired527();
            }
        }
        finally
        {
            callbackGate.ExitWriteLock();
        }
        foreach (NativeResourceType524 type in candidates) TryRetireEmpty527(type);
    }
}
