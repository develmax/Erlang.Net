namespace Erlang;

// A detached observation, never a parent physical-thread registration/authority.
internal sealed class ErlDriverAsyncAccountingSnapshot1126
{
    internal uint Identity { get; private set; }
    internal uint OperatingSystemThread { get; private set; }
    internal ulong Frequency { get; private set; }
    internal ulong PerformanceCounter { get; private set; }
    internal RuntimeMicrostate State { get; private set; }
    internal ulong[] Counters { get; } = new ulong[ErlDriverAsyncAccountingNames1126.States];

    internal void Bind(ErlDriverAsyncAccountingNative1126.Row row)
    {
        Identity = row.Identity;
        OperatingSystemThread = row.OperatingSystemThread;
        Frequency = row.Frequency;
        PerformanceCounter = row.PerformanceCounter;
        State = (RuntimeMicrostate)row.State;
        Counters[0] = row.Aux;
        Counters[1] = row.CheckIo;
        Counters[2] = row.Emulator;
        Counters[3] = row.Gc;
        Counters[4] = row.Other;
        Counters[5] = row.Port;
        Counters[6] = row.Sleep;
    }
}
