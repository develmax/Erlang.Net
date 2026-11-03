namespace Erlang;

internal sealed partial class ProcessTracing
{
    internal bool SupportsMessageSnapshots593()
    {
        lock (gate)
        {
            if (sendPattern is not null || receivePattern is not null || patterns.Count != 0
                || settings.Values.Any(value => (value.Flags & ProcessTraceFlags.Call) != 0))
                return false;

            return LiveTraceViews425().All(view => view.SupportsMessageSnapshots593());
        }
    }
}
