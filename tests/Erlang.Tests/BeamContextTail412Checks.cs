namespace Erlang.Tests;

internal static class BeamContextTail412Checks
{
    internal static async Task Transport()
    {
        BeamModule module = BeamModule.Prepare(BeamContextTail412Bytes.Image());
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (byte[] bytes in new byte[][] { [], [1], [0, 127, 255] })
        {
            BitString source = new(bytes);
            Term actual = await actor.InvokeAsync(context => module.Invoke(
                context, BeamOpcodeEdgeNames.Run, source).AsTask());
            BeamBinaryHostAccess.Require(actual.Equals(source));
        }
    }

    internal static Task Refusals()
    {
        foreach (byte[] image in new[] {
            BeamContextTail412Bytes.Image(contextArgument: false),
            BeamContextTail412Bytes.Image(overwrite: true),
            BeamContextTail412Bytes.Image(duplicate: true),
            BeamContextTail412Bytes.Image(position: true),
            BeamContextTail412Bytes.Image(cycle: true) })
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
            BeamBinaryHostAccess.Require(refused);
        }

        return Task.CompletedTask;
    }
}
