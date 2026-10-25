using System.Runtime.InteropServices;

namespace Erlang;

internal sealed partial class ErlDriverLinkedServices420
{
    private readonly object gate = new();
    private readonly LinkedList<ErlDriverLinkedRow420> entries = new();
    private readonly ErlDriverHostImages420 images;
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly ErlDriverAbiLayout420 layout;
    private readonly IErlDriverLinkedEntryHost420 host;
    private readonly ErlDriverServiceFault420 fault;

    internal ErlDriverLinkedServices420(
        ErlDriverHostImages420 images,
        ErlDriverThreadBridge420 bridge,
        ErlDriverAbiLayout420 layout,
        IErlDriverLinkedEntryHost420 host,
        ErlDriverServiceFault420 fault
    )
    {
        this.images = images;
        this.bridge = bridge;
        this.layout = layout;
        this.host = host;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.add_driver_entry)] = new ErlDriverCallbackTypes420.add_driver_entry(Add),
            [nameof(ErlDriverCallbackTypes420.remove_driver_entry)] = new ErlDriverCallbackTypes420.remove_driver_entry(Remove)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private static byte[] ReadName(nint pointer)
    {
        if (pointer == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        int size = 0;
        while (Marshal.ReadByte(pointer, size) != 0) size = checked(size + 1);
        var bytes = new byte[size];
        Marshal.Copy(
            pointer,
            bytes,
            0,
            size
        );

        return bytes;
    }

    private void Add(nint pointer)
    {
        try
        {
            if (pointer == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            ulong owner = bridge.ReadOwner();
            var image = images.WorkerImage(owner);
            var entry = new ErlDriverEntry420(pointer, layout);
            if (!entry.HasValidVersion) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            byte[] name = ReadName(entry.Name);
            using var initialization435 = BeginInitialization435();
            var lease = image.AcquireWorker();
            ErlDriverLinkedRow420 row;
            LinkedListNode<ErlDriverLinkedRow420> node;
            try
            {
                row = new(image, entry, lease);
                node = new(row);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
            // Like the source recursive driver-list lock, this is reentrant on
            // the actual native init thread. No image-list gate is held here.
            lock (gate)
            {
                var failures = new List<Exception>();
                bool keep = false;
                bool parentRegistered = false;
                try
                {
                    row.Identity = host.Register(new(owner, name, unchecked((uint)entry.Flags)), CurrentScope438());
                    parentRegistered = row.Identity != 0;
                    if (!parentRegistered) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    entries.AddFirst(node);
                    int result = entry.Callback<ErlDriverEntryTypes420.Initialize>(ErlDriverEntryNames420.Init)?.Invoke() ?? 0;
                    keep = result == 0;
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if (keep)
                {
                    initialization435.Commit();

                    return;
                }
                // Native init errors are intentionally ignored by add, but
                // each genuine owned descriptor/root is still rolled back.
                if (node.List is not null) entries.Remove(node);
                if (parentRegistered)
                {
                    try
                    {
                        host.Remove(row.Identity, CurrentScope438());
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
                }
                try
                {
                    lease.Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if (failures.Count != 0)
                {
                    FailLinkedVisibility438();
                    throw new AggregateException(failures);
                }
            }
        }
        catch (Exception error)
        {
            lock (gate)
                if (visibilityScope438 is not null) FailLinkedVisibility438();
            fault.Record(error);
        }
    }

    private int Remove(nint pointer)
    {
        try
        {
            lock (gate)
            {
                var node = entries.First;
                while (node is not null && node.Value.Entry.EntryPointer != pointer) node = node.Next;
                if (node is null) return images.IsDynamicEntry(pointer) ? -1 : 0;
                var row = node.Value;
                host.Remove(row.Identity, CurrentScope438());
                entries.Remove(node);
                row.Lease.Dispose();

                return 1;
            }
        }
        catch (Exception error)
        {
            lock (gate)
                if (visibilityScope438 is not null) FailLinkedVisibility438();
            fault.Record(error);

            return -1;
        }
    }

    internal ErlDriverHostSlot420? PortSlot(ulong identity)
    {
        lock (gate)
        {
            var row = entries.FirstOrDefault(entry => entry.Identity == identity);

            return row is null ? null : new(
                identity,
                row.Image,
                row.Entry,
                row.DriverGate
            );
        }
    }

    internal void RequireDrained()
    {
        lock (gate)
            if (entries.Count != 0) throw new InvalidOperationException(ErlDriverHostNames420.Closed);
    }
}
