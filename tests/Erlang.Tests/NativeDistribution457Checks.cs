using Erlang;

namespace Erlang.Tests;

internal static class NativeDistribution457Checks
{
    internal static async Task Run()
    {
        object wake = NativeDistribution457Access.NewWake();
        NativeDistribution457Access.Wake(wake);
        Task beforeCheck = NativeDistribution457Access.Capture(wake);
        Require(!beforeCheck.IsCompleted);
        var queue = new System.Collections.Concurrent.ConcurrentQueue<int>();
        await Task.Run(() => Produce(queue, wake, 1));
        Require(queue.TryDequeue(out int first) && first == 1);
        await beforeCheck.WaitAsync(NativeDistribution457Names.Timeout);
        Task afterEmpty = NativeDistribution457Access.Capture(wake);
        Require(!queue.TryDequeue(out _) && !afterEmpty.IsCompleted);
        await Task.Run(() => Produce(queue, wake, 2));
        await afterEmpty.WaitAsync(NativeDistribution457Names.Timeout);
        Require(queue.TryDequeue(out int second) && second == 2);
        Task shared = NativeDistribution457Access.Capture(wake);
        Require(ReferenceEquals(shared, NativeDistribution457Access.Capture(wake)));
        NativeDistribution457Access.Wake(wake);
        await shared.WaitAsync(NativeDistribution457Names.Timeout);
        Require(NativeDistribution457Access.Copy(NativeDistribution457Names.Bytes, [255], [1, 10, 2, 10, 3])
            .AsSpan().SequenceEqual(new byte[] { 255, 1, 10, 2, 10, 3 }));
        Require(NativeDistribution457Access.Copy(
            NativeDistribution457Names.Vector,
            [255],
            [],
            [[1, 10], [], [2, 10, 3]]
        )
            .AsSpan().SequenceEqual(new byte[] { 255, 1, 10, 2, 10, 3 }));
        Require(NativeDistribution457Access.Copy(NativeDistribution457Names.Bytes, [], []).Length == 0);
        Refuses(NativeDistribution457Names.Invalid, [], []);
        Refuses(NativeDistribution457Names.Vector, [], []);
        Refuses(
            NativeDistribution457Names.Vector,
            [],
            [1],
            [[2]]
        );
        Refuses(
            NativeDistribution457Names.Bytes,
            [],
            [],
            [[1]]
        );
    }

    private static void Produce(System.Collections.Concurrent.ConcurrentQueue<int> queue, object wake, int value)
    {
        queue.Enqueue(value);
        NativeDistribution457Access.Wake(wake);
    }

    private static void Refuses(
        string kind,
        byte[] header,
        byte[] body,
        byte[][]? segments = null
    )
    {
        bool refused = false;
        try
        {
            _ = NativeDistribution457Access.Copy(
                kind,
                header,
                body,
                segments
            );
        }
        catch (InvalidDataException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(NativeDistribution457Names.Failure);
    }
}
