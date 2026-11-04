namespace Erlang;

internal sealed class PollHarnessFixture1557 : IAsyncDisposable
{
    private readonly List<PollHarnessPipe1557> pipes = new(2);
    private readonly List<LinuxDescriptorPollOwner> owners = new(4);
    private LinuxDescriptorPollService? service;
    internal LinuxDescriptorPollService? ExistingEngine => service;
    internal RuntimeMicrostateAccounting Accounting { get; } = new();
    internal LinuxDescriptorPollService Engine => service ?? throw new InvalidOperationException(PollHarnessNames1557.Contract);
    internal PollHarnessPipe1557 Pipe => pipes[0];
    internal LinuxDescriptorPollProbe1557? Probe { get; private set; }

    internal async Task Initialize(bool start, bool probe)
    {
        if (probe) Probe = new();
        pipes.Add(new());
        service = new(Accounting, beforePoll: Probe is null ? null : Probe.BeforeNativePoll);
        if (start)
        {
            service.Start();
            await service.Ready.WaitAsync(PollHarnessNames1557.Deadline);
        }
    }

    internal PollHarnessPipe1557 NewPipe()
    {
        var pipe = new PollHarnessPipe1557();
        pipes.Add(pipe);

        return pipe;
    }

    internal LinuxDescriptorPollOwner Register(int descriptor)
    {
        var owner = Engine.RegisterBorrowed(descriptor);
        owners.Add(owner);

        return owner;
    }

    public async ValueTask DisposeAsync()
    {
        var errors = new List<Exception>();
        Probe?.Release();
        foreach (var owner in owners)
        {
            try
            {
                await owner.BeginRetire().WaitAsync(PollHarnessNames1557.Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (service is not null)
        {
            try
            {
                await service.DisposeAsync().AsTask().WaitAsync(PollHarnessNames1557.Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (service is null || service.PhysicalLifetimeEnded)
        {
            foreach (var pipe in pipes)
            {
                try
                {
                    pipe.Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            try
            {
                Probe?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        else errors.Add(new InvalidOperationException(PollHarnessNames1557.Contract)); // Retain FDs if native lifetime unknown.
        if (errors.Count != 0) throw new AggregateException(errors);
    }
}
