using Erlang.Differential;

namespace Erlang.Tests;

internal static class OracleExecutionPipelineTests
{
    public static async Task OrderingAndBound()
    {
        var gates = Enumerable.Range(0, 6).Select(_ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        int active = 0, peak = 0;

        async Task<string> Execute(string input, CancellationToken token)
        {
            int count = Interlocked.Increment(ref active);
            peak = Math.Max(peak, count);
            try
            {
                return await gates[int.Parse(input)].Task.WaitAsync(token);
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }
        await using var iterator = OracleExecutionPipeline.Run(
            Enumerable.Range(0, 6).ToArray(),
            index => index.ToString(),
            Execute,
            _ => { }
        ).GetAsyncEnumerator();
        var first = iterator.MoveNextAsync().AsTask();
        Require(active == 2);
        gates[1].SetResult("one");
        Require(!first.IsCompleted);
        gates[0].SetResult("zero");
        Require(await first && iterator.Current.Fixture == 0 && iterator.Current.Output == "zero");
        for (int index = 2; index < gates.Length; index++)
            gates[index].SetResult(index.ToString());
        for (int index = 1; index < gates.Length; index++)
            Require(await iterator.MoveNextAsync() && iterator.Current.Fixture == index);
        Require(!await iterator.MoveNextAsync() && active == 0 && peak == 2);
    }

    public static async Task OrderedFailureAndCleanup()
    {
        var first = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        int canceled = 0, starting = -1;

        async Task<string> Execute(string input, CancellationToken token)
        {
            if (input == "0")
                return await first.Task;
            if (input == "1")
                throw new InvalidDataException();
            try
            {
                await Task.Delay(Timeout.Infinite, token);

                return input;
            }
            finally
            {
                Interlocked.Increment(ref canceled);
            }
        }
        await using var iterator = OracleExecutionPipeline.Run(
            new[] { 0, 1, 2 },
            index => index.ToString(),
            Execute,
            index => starting = index
        ).GetAsyncEnumerator();
        var move = iterator.MoveNextAsync().AsTask();
        Require(starting == 0 && !move.IsCompleted);
        first.SetResult("first");
        Require(await move && iterator.Current.Fixture == 0);
        try
        {
            await iterator.MoveNextAsync();
            throw new InvalidOperationException();
        }
        catch (InvalidDataException)
        {
            Require(starting == 1 && canceled == 1);
        }
    }

    public static async Task EarlyDisposeCleanup()
    {
        int canceled = 0;

        async Task<string> Execute(string input, CancellationToken token)
        {
            if (input == "0")
                return input;
            try
            {
                await Task.Delay(Timeout.Infinite, token);

                return input;
            }
            finally
            {
                Interlocked.Increment(ref canceled);
            }
        }
        var iterator = OracleExecutionPipeline.Run(
            new[] { 0, 1, 2 },
            index => index.ToString(),
            Execute,
            _ => { }
        ).GetAsyncEnumerator();
        Require(await iterator.MoveNextAsync());
        await iterator.DisposeAsync();
        Require(canceled == 2);
    }

    public static async Task CallbackFailureCleanup()
    {
        int canceled = 0;

        async Task<string> Execute(string input, CancellationToken token)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token);

                return input;
            }
            finally
            {
                Interlocked.Increment(ref canceled);
            }
        }
        await using var iterator = OracleExecutionPipeline.Run(
            new[] { 0, 1, 2 },
            index => index.ToString(),
            Execute,
            _ => throw new InvalidDataException()
        ).GetAsyncEnumerator();
        try
        {
            await iterator.MoveNextAsync();
            throw new InvalidOperationException();
        }
        catch (InvalidDataException)
        {
            Require(canceled == 2);
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException();
    }
}
