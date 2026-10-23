using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class BeamTupleReceive454Checks
{
    internal static async Task Run()
    {
        BeamModule module = BeamModule.Prepare(Resource(BeamTupleReceive454Names.Image));
        await using var runtime = new ProcessRuntime();
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await module.Invoke(context, BeamTupleReceive454Names.Run);
                if (context.Mailbox.Count != 0) throw new InvalidOperationException(BeamTupleReceive454Names.Failure);
                observed.TrySetResult(result);

                return result;
            }
            catch (Exception failure)
            {
                observed.TrySetException(failure);
                throw;
            }
        });
        Term actual = await observed.Task;
        _ = await process.Completion;
        byte[] expected = Resource(BeamTupleReceive454Names.Expected);
        if (!actual.Equals(ExternalTermFormat.Decode(expected))
            || !ExternalTermFormat.Encode(actual).AsSpan().SequenceEqual(expected))
            throw new InvalidOperationException(BeamTupleReceive454Names.Failure);
    }

    private static byte[] Resource(string name)
    {
        using Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(BeamTupleReceive454Names.ResourcePrefix + name)
            ?? throw new InvalidOperationException(BeamTupleReceive454Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}
