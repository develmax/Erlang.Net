namespace Erlang;

internal sealed class DistributionOutputTransferScope : IDisposable
{
    [ThreadStatic]
    private static DistributionOutputTransferScope? current;
    private readonly RuntimeDistributionPending owner;
    private readonly DistributionOutputTransferScope? parent;
    private List<Action>? starts;
    private bool active = true;

    internal DistributionOutputTransferScope(RuntimeDistributionPending owner)
    {
        this.owner = owner;
        parent = current;
        current = this;
    }

    internal static bool HasActiveScope => current is { active: true };

    internal static bool TryAdd(RuntimeDistributionPending owner, Action start)
    {
        if (current is not { active: true } scope || !ReferenceEquals(scope.owner, owner))
            return false;
        (scope.starts ??= new()).Add(start);

        return true;
    }

    public void Dispose()
    {
        if (!active)
            return;
        active = false;
        current = parent;
        if (starts is null)
            return;
        if (parent is { active: true } && ReferenceEquals(parent.owner, owner))
            (parent.starts ??= new()).AddRange(starts);
        else
        {
            foreach (var start in starts)
                owner.StartTransferredOutput(start);
        }
        starts.Clear();
    }
}
