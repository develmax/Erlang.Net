using System.Runtime.ExceptionServices;

namespace Erlang;

// A borrowed snapshot keeps origin graph interests, not target Entry work.
// It cannot delay the target's per-recipient physical completion.
internal sealed class MailboxSnapshot601 : IDisposable
{
    private readonly Term[] values;
    private readonly IMailboxRootInterest593?[] interests;
    private readonly ModuleLiteralRoots801?[] literalRoots801;

    internal ModuleLiteralRoots801? RetainLiteralRoots801()
        => ModuleLiteralRoots801.RetainCombined801(literalRoots801.Take(count));

    private readonly Exception?[] failures;
    private int count, disposed;
    internal Exception? Failure { get; set; }
    internal IEnumerable<Term> Values => values.Take(count);
    internal int Count => count;

    internal MailboxSnapshot601(int capacity)
    {
        values = new Term[capacity];
        literalRoots801 = new ModuleLiteralRoots801?[capacity];
        interests = new IMailboxRootInterest593?[capacity];
        failures = new Exception?[capacity];
    }

    // Only called while the originating Entry owner is stable under M.
    internal void Add(Term value, MailboxGraphSnapshot593? graph)
    {
        IMailboxRootInterest593? interest = (IMailboxRootInterest593?)graph?.Retain();
        try
        {
            literalRoots801[count] = graph?.LiteralRoots801?.Retain801();
        }
        catch
        {
            // The originating Entry still holds its graph, so this is not the
            // final interest and cannot perform native retirement under M.
            _ = interest?.ReleaseLast();
            throw;
        }
        values[count] = value;
        interests[count] = interest;
        count++;
    }

    internal T With<T>(Func<T> complete)
    {
        Exception? primary = null;
        try
        {
            if (Failure is { } failure) ExceptionDispatchInfo.Capture(failure).Throw();

            return complete();
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                Dispose();
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        bool failed = false;
        // Every last-origin token is settled independently, outside R and M.
        for (int index = 0; index < count; index++)
        {
            try
            {
                MailboxRootSettlement593.Settle(Interlocked.Exchange(ref interests[index], null)?.ReleaseLast());
            }
            catch (Exception error)
            {
                failures[index] = error;
                failed = true;
            }
            try
            {
                Interlocked.Exchange(ref literalRoots801[index], null)?.Dispose();
            }
            catch (Exception error)
            {
                failures[index] = failures[index] is { } prior ? new AggregateException(prior, error) : error;
                failed = true;
            }
            values[index] = Nil.Value;
        }
        if (failed) throw new AggregateException(failures.OfType<Exception>());
    }
}
