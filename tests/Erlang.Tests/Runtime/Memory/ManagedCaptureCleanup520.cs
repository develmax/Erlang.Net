namespace Erlang.Tests;

internal static class ManagedCaptureCleanup520
{
    internal static async Task DisposeAsync(
        Exception? primary,
        Func<Task>? retire,
        IEnumerable<Action> disposeArtifacts,
        Action? beforeRetirement = null,
        Action<bool>? afterRetirement = null
    )
    {
        var failures = new List<Exception>();
        bool physicallyClean = true;
        try
        {
            beforeRetirement?.Invoke();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (retire is not null)
        {
            try
            {
                await retire();
            }
            catch (Exception error)
            {
                physicallyClean = false;
                failures.Add(error);
            }
        }
        foreach (Action dispose in disposeArtifacts)
        {
            try
            {
                dispose();
            }
            catch (Exception error)
            {
                physicallyClean = false;
                failures.Add(error);
            }
        }
        try
        {
            afterRetirement?.Invoke(physicallyClean);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count == 0)
            return;

        if (primary is not null)
            failures.Insert(0, primary);

        throw new AggregateException(failures);
    }
}
