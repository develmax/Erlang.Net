namespace Erlang;

// Actual ready frontend pin; unlike a CLR reference this blocks child shutdown.
internal sealed class RuntimeAsyncAccountingFrontendLease1170 : IDisposable
{
    private RuntimeDriverFrontend422? owner;
    internal bool Admitted;

    internal RuntimeAsyncAccountingFrontendLease1170(RuntimeDriverFrontend422 owner) => this.owner = owner;

    public void Dispose()
    {
        var current = Volatile.Read(ref owner);
        if (current is null) return;
        current.ReleaseAsyncAccounting1170(this, ref owner);
    }
}
