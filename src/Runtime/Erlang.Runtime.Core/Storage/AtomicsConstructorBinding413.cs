namespace Erlang;

internal sealed class AtomicsConstructorBinding413
{
    private readonly ModuleRegistry registry;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal AtomicsConstructorBinding413(ProcessRuntime runtime, ModuleRegistry registry)
    {
        Runtime = runtime;
        this.registry = registry;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(AtomicsNames413.Foreign, nameof(context));
        Runtime.RequireAtomicsActor413(context);
        using var invocation = Runtime.AtomicsOwner413.Enter();
        ulong length = AtomicsValues413.Word(arguments[0]);
        if (length == 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
        ulong options = AtomicsValues413.Word(arguments[1]);
        if (length > ulong.MaxValue / sizeof(long)) throw new ErlangException(ErlangErrorReasons.SystemLimit);
        AtomicsArray413 array = Runtime.AtomicsOwner413.Create(length, (options & AtomicsNames413.SignedOption) != 0);
        AtomicsBackingLease586? producer = null;
        bool producerEstablished = false;
        bool referenceRegistered586 = false;
        try
        {
            array.BindOwner586(Runtime.AtomicsOwner413, Runtime);
            producer = array.RetainBacking586(AtomicsLeaseKind586.Producer);
            producerEstablished = true;
            var root = new AtomicsGuestRoot413(Runtime.AtomicsOwner413, array);
            ResourceReferenceTerm reference = Runtime.ReferenceResources.CreateAtomics413(root);
            referenceRegistered586 = true;
            // PARENT SHARED-ROOT SEAM: actual guest header OR per-wrapper host
            // carrier must commit before this provisional producer can drop.
            ResourceReferenceTerm admitted = context.AdmitAtomicsProducer586(reference, root);
            producer.Dispose();
            producer = null;

            return ValueTask.FromResult<Term>(admitted);
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
            if (!producerEstablished || !referenceRegistered586)
            {
                try
                {
                    Runtime.AtomicsOwner413.Retire(array);
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
