namespace Erlang;

internal sealed class ProcessGarbageCollectionPolicy
{
    private readonly object gate = new();
    private ushort threshold = ProcessFullSweepNames.DefaultThreshold;
    private ushort minorCollections;

    internal ushort Threshold
    {
        get
        {
            lock (gate)
                return threshold;
        }
    }

    internal ushort ExchangeThreshold(ushort value)
    {
        lock (gate)
        {
            ushort previous = threshold;
            threshold = value;

            return previous;
        }
    }

    // State advances only after a real collection completes; a failing collector leaves it unchanged.
    internal bool Collect(bool requestedMajor, Action<bool> collector)
    {
        lock (gate)
        {
            bool major = requestedMajor || minorCollections >= threshold;
            collector(major);
            minorCollections = major ? (ushort)0 : checked((ushort)(minorCollections + 1));

            return major;
        }
    }
}
