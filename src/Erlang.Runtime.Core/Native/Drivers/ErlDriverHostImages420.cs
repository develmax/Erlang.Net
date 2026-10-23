namespace Erlang;

// Child-private authenticated objects, never a second VM's Windows DLL list.
// Main VM DDLL state chooses when to load/unload; this owner performs the real
// native admission, callback scopes and physical image retirement.
internal sealed class ErlDriverHostImages420
{
    private readonly object gate = new();
    private readonly SemaphoreSlim operations = new(1, 1);
    private readonly Dictionary<ulong, ErlDriverHostImageRow420> images = new();
    private ErlDriverThreadBridge420? bridge;
    private ErlDriverCallbackTable420? callbacks;
    private ErlDriverAbiLayout420? layout;
    private ulong lastIdentity;
    private bool closing;
    private Func<ErlDriverLinkedInitialization435>? beginInitialization435;

    internal void Configure(
        ErlDriverThreadBridge420 bridge,
        ErlDriverCallbackTable420 callbacks,
        ErlDriverAbiLayout420 layout,
        Func<ErlDriverLinkedInitialization435> beginInitialization435
    )
    {
        lock (gate)
        {
            if (this.bridge is not null || closing)
                throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            this.bridge = bridge;
            this.callbacks = callbacks;
            this.layout = layout;
            this.beginInitialization435 = beginInitialization435 ?? throw new ArgumentNullException(nameof(beginInitialization435));
        }
    }

    internal ErlDriverImage420 WorkerImage(ulong identity)
    {
        lock (gate)
        {
            // Native workers can be created by entry.init and finish. Keep the
            // genuine staging/retiring owner until its last OS thread exits.
            if (!images.TryGetValue(identity, out var row) || row.Image is null)
                throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);

            return row.Image;
        }
    }

    internal ErlDriverImage420 PortImage(ulong identity)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (!images.TryGetValue(identity, out var row) || !row.Published || row.Image is null)
                throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);

            return row.Image;
        }
    }

    internal ErlDriverHostSlot420 PortSlot(ulong identity)
    {
        var image = PortImage(identity);

        return new(
            identity,
            image,
            image.Entry,
            image.DriverGate
        );
    }

    internal bool IsDynamicEntry(nint pointer)
    {
        lock (gate)
            return images.Values.Any(row => row.Image is not null && row.Image.Entry is not null
                && row.Image.Entry.EntryPointer == pointer);
    }

    internal async ValueTask<int> LoadAsync(
        ulong identity,
        byte[] name,
        byte[] nativePath,
        bool schedulerThread
    )
    {
        await operations.WaitAsync().ConfigureAwait(false);
        ErlDriverHostImageRow420? row = null;
        try
        {
            ErlDriverCallbackTable420 table;
            ErlDriverAbiLayout420 abi;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, this);
                if (callbacks is null || layout is null || bridge is null || identity == 0 || identity <= lastIdentity)
                    throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
                table = callbacks;
                abi = layout;
                row = new(identity);
                images.Add(identity, row);
                lastIdentity = identity;
            }
            var path = ErlDriverPath420.FromWire(nativePath);
            var admitted = await ErlDriverImage420.LoadAsync(
                table,
                abi,
                path.WindowsPath,
                path.ByteCount,
                name,
                image => Enter(row, image),
                schedulerThread,
                beginInitialization435 ?? throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame)
            ).ConfigureAwait(false);
            if (admitted.Owner is null)
            {
                lock (gate) images.Remove(identity);

                return admitted.Error;
            }
            lock (gate)
            {
                // Close waits this real in-progress operation before collecting
                // owners; no image escapes shutdown between init and publication.
                row.Image = admitted.Owner;
                row.Published = true;
            }

            return ErlDriverLoadErrors420.NoError;
        }
        catch
        {
            if (row is not null)
            {
                // LoadAsync itself joins failed native initialization cleanup.
                lock (gate) images.Remove(identity);
            }
            throw;
        }
        finally
        {
            operations.Release();
        }
    }

    private IDisposable Enter(ErlDriverHostImageRow420 row, ErlDriverImage420 image)
    {
        ErlDriverThreadBridge420 provider;
        lock (gate)
        {
            if (!images.TryGetValue(row.Identity, out var current) || !ReferenceEquals(current, row)
                || row.Image is not null && !ReferenceEquals(row.Image, image))
                throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            row.Image = image;
            provider = bridge ?? throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
        }

        return new ErlDriverOwnerScope420(provider, row.Identity);
    }

    internal async ValueTask UnloadAsync(ulong identity)
    {
        await operations.WaitAsync().ConfigureAwait(false);
        try
        {
            ErlDriverHostImageRow420 row;
            lock (gate)
            {
                if (!images.TryGetValue(identity, out row!) || row.Image is null || !row.Published)
                    throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
                row.Published = false;
            }
            await row.Image.UnloadAsync().ConfigureAwait(false);
            lock (gate) images.Remove(identity);
        }
        finally
        {
            operations.Release();
        }
    }

    internal async Task CloseAsync()
    {
        lock (gate) closing = true;
        await operations.WaitAsync().ConfigureAwait(false);
        try
        {
            ErlDriverHostImageRow420[] owned;
            lock (gate)
            {
                owned = images.Values.ToArray();
                foreach (var row in owned) row.Published = false;
            }
            var failures = new List<Exception>();
            foreach (var row in owned)
            {
                try
                {
                    if (row.Image is not null) await row.Image.UnloadAsync().ConfigureAwait(false);
                    lock (gate) images.Remove(row.Identity);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
        finally
        {
            operations.Release();
        }
    }
}
