namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    internal void ChildPhysicallySettled(Exception failure)
    {
        lock (gate)
        {
            closing = true;
            foreach (var row in identities.Values)
            {
                row.Processes.Clear();
                row.Ports.Clear();
                row.ReloadPath = null;
                row.Path = null;
            }
            drivers.Clear();
            identities.Clear();
            retiring.Clear();
            // Unknown native cleanup is not a successful unloaded notification.
            // Existing native calls will observe the actual aborted peer;
            // pending stable waiters must not hang on a lost physical owner.
            mutation?.TrySetException(failure);
            wake.Release();
        }
    }
}
