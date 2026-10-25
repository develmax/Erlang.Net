namespace Erlang;

public sealed partial class HostDistribution
{
    internal HostDistributionHandle AttachNative457(string channel, NativeDistributionBinding457 binding)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (entries.ContainsKey(channel) || !binding.IsLive)
                throw new InvalidOperationException(HostDistributionDiagnostics.Attached);
            var entry = new HostDistributionEntry(channel, checked(++generation), null) { NativePort457 = binding };
            entries.Add(channel, entry);

            return new(this, entry);
        }
    }

    internal HostDistributionBuffer? TakeNative457(HostDistributionHandle handle, NativeDistributionBinding457 binding)
    {
        lock (gate)
        {
            var entry = Require(handle);
            if (!ReferenceEquals(entry.NativePort457, binding))
                throw new InvalidOperationException(HostDistributionDiagnostics.Controller);

            return TakeBuffer457(entry, out _);
        }
    }

    internal void RecordNativeInput457(HostDistributionHandle handle, NativeDistributionBinding457 binding)
    {
        lock (gate)
        {
            var entry = Require(handle);
            if (!ReferenceEquals(entry.NativePort457, binding))
                throw new InvalidOperationException(HostDistributionDiagnostics.InputHandler);
            entry.InputPackets = checked(entry.InputPackets + 1);
        }
    }
}
