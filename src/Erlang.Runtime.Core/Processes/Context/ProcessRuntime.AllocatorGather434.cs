using System.Numerics;
namespace Erlang;

public sealed partial class ProcessRuntime
{
    private AllocatorGatherOwner434? allocatorGather434;
    private bool allocatorGatherClosed434;

    internal RuntimeSchedulerAuxiliaryBatch420 PrepareAllocatorAux434(Action<int> body) => actorDispatcher.PrepareAuxiliary420(body);

    internal AllocatorBlockWindow434[] CaptureAllocatorWindow434(string allocator)
    {
        NifNativeAllocationDomain? nif;
        RuntimeCountersOwner421? counters;
        RuntimePersistentTerms? persistent;
        lock (gate)
        {
            nif = nativeAllocationDomain;
            counters = counters421;
            persistent = persistentTerms;
        }

        return allocator switch
        {
            AllocatorBackendNames.Driver => nif?.CaptureWindow434() ?? [],
            AllocatorBackendNames.Standard => atomics413.CaptureWindow434().Concat(counters?.CaptureWindow434() ?? []).ToArray(),
            AllocatorBackendNames.Literal or AllocatorBackendNames.LongLived => persistent?.CaptureAllocatorWindow434().Where(block => block.Allocator == allocator).ToArray() ?? [],
            _ => throw new NotSupportedException(AllocatorGatherNames434.Frontier)
        };
    }

    internal Term GatherAllocator434(ProcessContext context, Term argument, bool carriers)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new InvalidOperationException(AllocatorGatherNames434.Foreign);
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(allocatorGatherClosed434, this);
            RequireExternalTermDomain(argument);
            if (argument is not TupleTerm { Items.Count: 6 } tuple || tuple.Items[0] is not Atom allocator || !AllocatorGatherNames434.Allocators.Contains(allocator.Name))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            long index = Small(tuple.Items[1]), widthValue = Small(tuple.Items[2]), startValue = Small(tuple.Items[3]), flags = Small(tuple.Items[4]);
            if (index < 0 || index >= actorDispatcher.AuxiliaryParticipants430() || tuple.Items[5] is not ReferenceTerm reference || !IsCurrentLocal(reference))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            // Real own backend has global, thread-safe owner instances, no
            // separate per-normal/dirty allocator instances. No invented lanes.
            if (index != 0) return Term.I(0);
            if (allocator.Name == AllocatorBackendNames.Binary) throw new NotSupportedException(AllocatorGatherNames434.Frontier);
            if (allocator.Name == AllocatorBackendNames.Driver && driverFrontend422 is not null)
                throw new NotSupportedException(AllocatorGatherNames434.Frontier);
            bool enabled = allocator.Name switch
            {
                AllocatorBackendNames.Standard => true,
                AllocatorBackendNames.Driver => nativeAllocationDomain is not null,
                AllocatorBackendNames.Literal or AllocatorBackendNames.LongLived => persistentTerms is not null,
                _ => false // CLR managed roots/global sys allocator are not ALCU instances.
            };
            if (!enabled) return Term.I(0);
            if (widthValue < 0 || widthValue > Array.MaxLength || !carriers && startValue == 0)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var request = new AllocatorGatherRequest434(
                context,
                reference,
                allocator.Name,
                checked((int)widthValue),
                unchecked((ulong)startValue),
                unchecked((int)flags),
                carriers
            );
            (allocatorGather434 ??= new(this, gate)).Publish(request);

            return Term.I(1);
        }
    }

    private static long Small(Term value)
    {
        if (value is not Integer integer || integer.Value < -(BigInteger.One << 59) || integer.Value >= (BigInteger.One << 59))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (long)integer.Value;
    }

    private Task CloseAllocatorGather434()
    {
        allocatorGatherClosed434 = true;

        return allocatorGather434?.Close() ?? Task.CompletedTask;
    }

    private Task DrainAllocatorGather434()
    {
        lock (gate) return CloseAllocatorGather434();
    }

    private bool AllocatorGatherRetirementCompleted434 => allocatorGather434 is null || allocatorGather434.Drained;

    private Task AllocatorGatherProcessRetirement434(ProcessContext context, Task previous)
        => Task.WhenAll(
            previous,
            allocatorGather434?.ProcessRetirement(context) ?? Task.CompletedTask,
            context.Self.Equals(InitializationPid) ? CloseAllocatorGather434() : Task.CompletedTask
        );
}
