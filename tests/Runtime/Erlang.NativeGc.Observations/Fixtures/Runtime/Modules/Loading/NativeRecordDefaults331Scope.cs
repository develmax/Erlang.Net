using static Erlang.Tests.NativeRecordDefaults331Names;

namespace Erlang.Tests;

internal sealed class NativeRecordDefaults331Scope
{
    private readonly List<IAsyncDisposable> resources = new();

    internal T Own<T>(T resource) where T : IAsyncDisposable
    {
        try
        {
            resources.Add(resource);
        }
        catch (Exception primary)
        {
            try
            {
                resource.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }

        return resource;
    }

    internal static async Task Attempt(List<Exception> failures, string label, Func<NativeRecordDefaults331Scope, Task> action)
    {
        int previous = failures.Count;
        var scope = new NativeRecordDefaults331Scope();
        try
        {
            await action(scope);
        }
        catch (Exception original)
        {
            failures.Add(new InvalidOperationException(label + ExecutionFailure, original));
        }
        for (int index = scope.resources.Count - 1; index >= 0; index--)
        {
            IAsyncDisposable resource = scope.resources[index];
            try
            {
                await resource.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                failures.Add(new InvalidOperationException(label + CleanupFailure + index + "/" + resource.GetType().FullName, cleanup));
            }
        }
        Console.WriteLine((failures.Count == previous ? Passed : Failed) + label);
    }
}
