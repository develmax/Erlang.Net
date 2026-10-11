namespace Erlang.Differential;

public static class OracleExecutionPipeline
{
    public static async IAsyncEnumerable<OracleExecutionResult<T>> Run<T>(
        IReadOnlyList<T> fixtures,
        Func<T, string> command,
        Func<string, CancellationToken, Task<string>> execute,
        Action<T> starting
    )
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new Queue<(T Fixture, Task<string> Output)>();
        var running = new HashSet<Task<string>>();
        int next = 0;

        async Task<string> Start(T fixture) => await execute(command(fixture), cancellation.Token);

        void Fill()
        {
            while (next < fixtures.Count && pending.Count < OracleExecutionLimits.MaximumConcurrentProcesses)
            {
                T fixture = fixtures[next++];
                Task<string> output = Start(fixture);
                running.Add(output);
                pending.Enqueue((fixture, output));
            }
        }
        try
        {
            Fill();
            while (pending.TryDequeue(out var item))
            {
                starting(item.Fixture);
                string output = await item.Output;
                running.Remove(item.Output);
                Fill();

                yield return new(item.Fixture, output);
            }
        }
        finally
        {
            cancellation.Cancel();
            foreach (var output in running)
            {
                try
                {
                    await output;
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
