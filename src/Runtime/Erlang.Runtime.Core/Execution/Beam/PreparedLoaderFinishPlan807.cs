using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class PreparedLoaderFinishPlan807 : IDisposable
{
    private readonly RuntimeReferenceResources resources;
    private PreparedLoaderReservation807?[]? owned;
    private readonly List<Exception> cleanupErrors;
    private readonly PreparedLoaderWaiting807?[]? waiting807;
    private ExceptionDispatchInfo? pendingError;
    private int committed807;
    private PreparedBeamImage[] images = [];

    private PreparedLoaderFinishPlan807(RuntimeReferenceResources resources, int capacity, bool waiting = false)
    {
        this.resources = resources;
        owned = new PreparedLoaderReservation807[capacity];
        cleanupErrors = new(checked(capacity * 2));
        if (waiting) waiting807 = new PreparedLoaderWaiting807[capacity];
    }

    internal static PreparedLoaderFinishPlan807 Capture807(RuntimeReferenceResources resources, Term list)
    {
        Term[] terms;
        try
        {
            terms = Cons.Items(list).ToArray();
        }
        catch (Exception error)
        {
            return new(resources, 0) { pendingError = ExceptionDispatchInfo.Capture(error) };
        }

        return CaptureTerms807(resources, terms);
    }

    internal static PreparedLoaderFinishPlan807 CaptureReferences807(RuntimeReferenceResources resources, IReadOnlyList<ReferenceTerm> references)
        => CaptureTerms807(resources, references.Cast<Term>().ToArray(), true);

    private static PreparedLoaderFinishPlan807 CaptureTerms807(RuntimeReferenceResources resources, IReadOnlyList<Term> terms, bool waiting = false)
    {
        var plan = new PreparedLoaderFinishPlan807(resources, terms.Count, waiting);
        try
        {
            plan.images = new PreparedBeamImage[terms.Count];
            for (int index = 0; index < terms.Count; index++)
            {
                if (terms[index] is not ReferenceTerm reference)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (waiting)
                {
                    plan.waiting807![index] = resources.ReservePreparedWaiting807(reference)
                        ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
                    continue;
                }
                PreparedLoaderReservation807? operation = resources.ReservePreparedNative807(reference);
                if (operation is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
                plan.owned![index] = operation;
                plan.images[index] = operation.Image807;
            }
        }
        catch (Exception error)
        {
            // Original argument/identity errors surface only at the original
            // permission-controlled callback. Cancellation/admission can win
            // before that callback exactly as before this preparation split.
            plan.pendingError = ExceptionDispatchInfo.Capture(error);
        }

        return plan;
    }

    internal Term Commit807(ModuleRegistry registry) => registry.CommitPreparedFinish807(this);

    internal Term CommitUnderGate807(ModuleRegistry registry)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref owned) is null, PreparedLoaderNames807.Closed);
        registry.RequireSourceMutationScope();
        if (Interlocked.Exchange(ref committed807, 1) != 0)
            throw new InvalidOperationException(PreparedLoaderNames807.PlanUsed);
        // Legacy Host waiters recheck closure before any native payload read.
        // VM close may have invalidated their Host custody while M was blocked.
        if (waiting807 is not null) resources.RequirePreparedFinishOpen807(images.Length);
        pendingError?.Throw();
        resources.RequirePreparedFinishOpen807(images.Length);
        if (waiting807 is not null)
            for (int index = 0; index < waiting807.Length; index++)
            {
                owned![index] = resources.UpgradePreparedWaiting807(waiting807[index]!);
                images[index] = owned[index]!.Image807;
            }

        return registry.FinishLoaderImages(images);
    }

    internal async ValueTask<Term> Run807(ProcessContext context)
    {
        Term? result = null;
        Exception? primary = null;
        try
        {
            result = await context.Runtime.Modules.ModifySourceCodeAsync(context, () => Commit807(context.Runtime.Modules));
        }
        catch (Exception error)
        {
            primary = error;
        }
        try
        {
            Dispose();
        }
        catch (Exception cleanup)
        {
            if (primary is not null) throw new AggregateException(primary, cleanup);
            throw;
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();

        return result!;
    }

    public void Dispose()
    {
        PreparedLoaderReservation807?[]? leases = Interlocked.Exchange(ref owned, null);
        if (leases is null) return;
        foreach (PreparedLoaderReservation807? lease in leases)
            try
            {
                lease?.Dispose();
            }
            catch (Exception error)
            {
                cleanupErrors.Add(error);
            }
        if (waiting807 is not null)
            foreach (PreparedLoaderWaiting807? waiting in waiting807)
                try
                {
                    waiting?.Dispose();
                }
                catch (Exception error)
                {
                    cleanupErrors.Add(error);
                }
        if (cleanupErrors.Count != 0) throw new AggregateException(cleanupErrors);
    }
}
