namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeProcessPayloadOwner641? processPayloads641;
    internal RuntimeProcessPayloadOwner641 ProcessPayloads641
    {
        get
        {
            lock (gate) return processPayloads641 ??= new RuntimeProcessPayloadOwner641(this);
        }
    }

    private void StopProcessPayloadAdmission641() => ProcessPayloads641.Stop641();

    private Task DrainProcessPayloads641() => ProcessPayloads641.Drain641();

    private bool ProcessPayloadRetirementCompleted641(ProcessContext context)
        => processPayloads641?.HasContextRetirement641(context) != true;

    private Task ProcessPayloadRetirement641(ProcessContext context, Task preceding)
    {
        var owner = ProcessPayloads641;
        owner.SealContext641(context);
        Task own = owner.JoinContext641(context);

        return Task.WhenAll(preceding, own);
    }
}
