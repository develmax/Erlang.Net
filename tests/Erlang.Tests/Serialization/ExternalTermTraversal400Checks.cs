using Erlang;
using static Erlang.Tests.ExternalTermTraversal400Names;

namespace Erlang.Tests;

internal static class ExternalTermTraversal400Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        Term remote = new Pid(RemoteNode, 1);
        Term branch = Term.Tuple(Term.A(Value), remote);
        var wide = new TupleTerm(Enumerable.Repeat(branch, Width));
        using (RuntimeExternalTermReservation warm = runtime.ReserveExternalTerm(branch))
            Require(ReferenceEquals(warm.Value, branch));
        long before = GC.GetAllocatedBytesForCurrentThread();
        using (RuntimeExternalTermReservation retained = runtime.ReserveExternalTerm(wide))
            Require(ReferenceEquals(retained.Value, wide));
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated < AuxiliaryAllocationBudgetBytes);
        Term invalid = Term.Tuple(wide, Term.List(Term.Tuple(foreign.InitializationPid)));
        try
        {
            using RuntimeExternalTermReservation accepted = runtime.ReserveExternalTerm(invalid);
            throw new InvalidOperationException(Failure);
        }
        catch (ArgumentException)
        {
        }
        using RuntimeExternalTermReservation again = runtime.ReserveExternalTerm(wide);
        Require(ReferenceEquals(again.Value, wide));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            Role = ExternalTermTraversal400Names.Test,
            Width,
            AuxiliaryAllocatedBytes = allocated,
            BudgetBytes = AuxiliaryAllocationBudgetBytes,
            HostTraversalEvidenceOnly = true
        }));
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
