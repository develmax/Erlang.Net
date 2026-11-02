namespace Erlang;

internal sealed class PreparedHamtBitmap785 : IDisposable
{
    private readonly object gate = new();
    private HamtNodeRead785? reader;
    private bool disposed;
    private int users;
    internal ProcessRuntime Runtime { get; }
    internal HamtBitmapTerm429 Source { get; }
    internal Term[] Children { get; }
    internal int PairCount { get; }

    internal PreparedHamtBitmap785(ProcessRuntime runtime, HamtBitmapTerm429 source)
    {
        Runtime = runtime;
        Source = source;
        HamtNodeRead785 admitted = runtime.AcquireHamtNode785(source);
        try
        {
            Children = source.Children.ToArray();
            int pairs = 0;
            var pending = new Stack<Term>(Children);
            while (pending.TryPop(out Term? value))
            {
                switch (value)
                {
                    case Cons:
                        pairs = checked(pairs + 1);
                        break;
                    case TupleTerm collision when collision.Items.Count >= 2:
                        foreach (Term child in collision.Items)
                        {
                            if (child is not Cons)
                                throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
                            pairs = checked(pairs + 1);
                        }
                        break;
                    case HamtBitmapTerm429 bitmap:
                        runtime.RequireHamtNode429(bitmap);
                        foreach (Term child in bitmap.Children) pending.Push(child);
                        break;
                    default:
                        throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
                }
            }
            PairCount = pairs;
            reader = admitted;
        }
        catch (Exception primary)
        {
            try
            {
                admitted.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    internal PreparedHamtBitmapUse785 Enter()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var use = new PreparedHamtBitmapUse785(this);
            users = checked(users + 1);

            return use;
        }
    }

    internal void Leave()
    {
        HamtNodeRead785? release = null;
        lock (gate)
        {
            users--;
            if (disposed && users == 0)
            {
                release = reader;
                reader = null;
            }
        }
        release?.Dispose();
    }

    public void Dispose()
    {
        HamtNodeRead785? release = null;
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            if (users == 0)
            {
                release = reader;
                reader = null;
            }
        }
        release?.Dispose();
    }
}
