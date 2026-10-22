namespace Erlang;

// The map/node graph owns this token. The owner's directory holds only a row
// without term payload or the token; weak-map collection can really retire it.
internal sealed class HamtHashLeases429(HamtHashLeaseRow429 row) : IDisposable
{
    internal HamtHashLeaseRow429 Row => row;

    public void Dispose()
    {
        try
        {
            row.BeginRelease().GetAwaiter().GetResult();
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    ~HamtHashLeases429()
    {
        // No waiting and no thrown finalizer. Explicit owner Close joins the
        // exact same row completion, including a concurrent finalizer release.
        try
        {
            _ = row.BeginRelease();
        }
        catch (Exception error)
        {
            row.RecordUnexpectedFailure(error);
        }
    }
}
