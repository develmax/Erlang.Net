using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class BeamContextForward397Checks
{
    internal static async Task OptionalAndRequired()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (bool required in new[] { false, true })
        {
            BeamModule module = BeamModule.Prepare(BeamContextForward397Bytes.Image(required: required));
            Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
            BitString source = new([1, 127, 255]);
            Require((await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, source).AsTask())).Equals(source));
        }
        BeamModule ordinary = BeamModule.Prepare(BeamContextForward397Bytes.Image(contextArgument: false));
        BitString bytes = new([3, 4]);
        Require((await actor.InvokeAsync(context => ordinary.Invoke(context, BeamOpcodeEdgeNames.Run, bytes).AsTask())).Equals(bytes));
    }

    internal static Task Refusals()
    {
        foreach (byte[] image in new[] {
            BeamContextForward397Bytes.Image(required: true, contextArgument: false),
            BeamContextForward397Bytes.Image(missingRoot: true),
            BeamContextForward397Bytes.Image(overwriteX: true),
            BeamContextForward397Bytes.Image(positionArgument: true),
            BeamContextForward397Bytes.Image(nativeEscape: true),
            BeamContextForward397Bytes.Image(cycle: true) })
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
            Require(refused);
        }

        return Task.CompletedTask;
    }
}
