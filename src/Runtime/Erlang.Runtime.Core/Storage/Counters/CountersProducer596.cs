namespace Erlang;

internal static class CountersProducer596
{
    internal static ResourceReferenceTerm Create(
        ProcessRuntime owner,
        RuntimeCountersOwner421 state,
        ProcessContext context,
        ulong length
    )
    {
        CountersArray421 array = state.Create(length, owner.ActorDispatchCapacity);
        CountersBackingLease596? producer = null;
        bool producerEstablished = false;
        bool referenceRegistered596 = false;
        try
        {
            array.BindOwner596(state, owner);
            producer = array.RetainBacking596(CountersLeaseKind596.Producer);
            producerEstablished = true;
            var root = new CountersGuestRoot421(state, array);
            ResourceReferenceTerm reference = owner.ReferenceResources.CreateCounters421(root);
            referenceRegistered596 = true;
            // PARENT SHARED-ROOT SEAM: actual guest header OR per-wrapper host
            // carrier must commit before this provisional producer can drop.
            ResourceReferenceTerm admitted = context.AdmitCountersProducer596(reference, root);
            producer.Dispose();
            producer = null;

            return admitted;
        }
        catch (Exception original)
        {
            // Cleanup must not allocate before releasing actual ownership.
            // The original failure may itself be an allocation failure.
            Exception? producerFailure = null;
            Exception? retirementFailure = null;
            try
            {
                producer?.Dispose();
            }
            catch (Exception cleanup)
            {
                producerFailure = cleanup;
            }
            // Once explicit ownership began, last-root release queues retirement.
            // Do not synchronously Close: a failed publication may already own
            // a real header in this invocation that unwinds only after we throw.
            // Registration failure cannot have published a process header.
            // Complete its rollback before reporting failure to the caller.
            if (!producerEstablished || !referenceRegistered596)
            {
                try
                {
                    state.Retire(array);
                }
                catch (Exception cleanup)
                {
                    retirementFailure = cleanup;
                }
            }
            // Any failure aggregation happens only after every required cleanup
            // phase has been attempted, even if aggregation itself cannot allocate.
            if (producerFailure is not null)
            {
                if (retirementFailure is not null)
                    throw new AggregateException(original, producerFailure, retirementFailure);

                throw new AggregateException(original, producerFailure);
            }
            if (retirementFailure is not null)
                throw new AggregateException(original, retirementFailure);
            throw;
        }
    }
}
