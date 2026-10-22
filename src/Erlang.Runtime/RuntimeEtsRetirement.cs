namespace Erlang;

internal sealed class RuntimeEtsRetirement(
    RuntimeEtsFoundation owner,
    LinkedList<RuntimeEtsOwnerTables> groups,
    Pid? actor
)
{
    private readonly List<Exception> failures = new();

    internal LinkedList<RuntimeEtsOwnerTables> Groups { get; } = groups;
    internal Pid? Actor { get; } = actor;
    internal LinkedList<RuntimeEtsObjectSlot>? Objects { get; set; }
    internal LinkedList<RuntimeEtsFixation420>? Fixations420 {get;set;}
    internal Task Execution { get; set; } = Task.CompletedTask;

    internal async Task ExecuteAsync()
    {
        // Register ownership before the first turn, including during runtime shutdown.
        try
        {
            await Task.Yield();
            int units = 0;
            while (owner.RetireNext(this, out RuntimeEtsStoredObject? value, out RuntimeEtsHeirTransfer419? transfer))
            {
                try
                {
                    if(transfer is not null)owner.CompleteHeirTransfer419(this,transfer);
                    else value?.Release();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if (++units == RuntimeEtsRetirementParameters.UnitsPerTurn)
                {
                    units = 0;
                    owner.RetirementYielded();
                    await Task.Yield();
                }
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            owner.RetirementEnded(this, failures);
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }
}
