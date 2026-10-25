using Erlang;

namespace Erlang.Tests;

internal static class BeamNoReturnSwap481Checks
{
    internal static async Task Execution()
    {
        BeamModule module = BeamModule.Prepare(BeamNoReturnSwap481Bytes.Image());
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, Nil.Value).AsTask());
        BeamBinaryHostAccess.Require(value.Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
    }

    internal static Task Refusals()
    {
        foreach (var (image, failure) in new[] {
            (BeamNoReturnSwap481Bytes.Image(returning: true), BeamNoReturnSwap481Names.Returning),
            (BeamNoReturnSwap481Bytes.Image(uninitialized: true), BeamNoReturnSwap481Names.Uninitialized),
            (BeamNoReturnSwap481Bytes.Image(unknown: true), BeamNoReturnSwap481Names.Unknown) })
        {
            bool refused;
            try
            {
                refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            if (!refused)
                throw new InvalidOperationException(failure);
        }

        return Task.CompletedTask;
    }
}
