namespace Erlang;

public static class TermMapComparisonEnvironment
{
    [ThreadStatic]
    private static Func<ITermMapComparisonLayout>? current;

    internal static Func<ITermMapComparisonLayout>? Factory => current;

    // This scope surrounds one synchronous admitted actor turn, never an await.
    // The next actor continuation installs its own runtime before running again.
    public static TermMapComparisonScope EnterThread(Func<ITermMapComparisonLayout> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var previous = new TermMapComparisonScope(current, Environment.CurrentManagedThreadId);
        current = factory;

        return previous;
    }

    internal static void Restore(Func<ITermMapComparisonLayout>? previous) => current = previous;
}
