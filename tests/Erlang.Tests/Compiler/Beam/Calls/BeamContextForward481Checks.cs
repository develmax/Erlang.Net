using Erlang;

namespace Erlang.Tests;

internal static class BeamContextForward481Checks
{
    internal static async Task Transport()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (bool atom in new[] { false, true })
            foreach (bool required in new[] { false, true })
                foreach (bool context in new[] { false, true })
                {
                    if (required && !context)
                        continue;
                    BeamModule module = BeamModule.Prepare(BeamContextForward481Bytes.Image(contextArgument: context, atom: atom, required: required));
                    BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
                    foreach (byte[] bytes in new byte[][] { [], [1], [0, 127, 255] })
                    {
                        BitString source = new(bytes);
                        Term actual = await actor.InvokeAsync(process => module.Invoke(process, BeamOpcodeEdgeNames.Run, source).AsTask());
                        BeamBinaryHostAccess.Require(actual.Equals(source));
                    }
                }

        foreach (bool atom in new[] { false, true })
        {
            BeamModule module = BeamModule.Prepare(BeamContextForward481Bytes.Image(
                contextArgument: false,
                duplicate: true,
                atom: atom,
                required: false
            ));
            BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
            BitString source = new(new byte[] { 0, 127, 255 });
            Term actual = await actor.InvokeAsync(process => module.Invoke(process, BeamOpcodeEdgeNames.Run, source).AsTask());
            BeamBinaryHostAccess.Require(actual.Equals(source));
        }
    }

    internal static Task Refusals()
    {
        foreach (bool atom in new[] { false, true })
            foreach (byte[] image in new[] {
            BeamContextForward481Bytes.Image(contextArgument: false, atom: atom),
            BeamContextForward481Bytes.Image(overwrite: true, atom: atom),
            BeamContextForward481Bytes.Image(overwrite: true, atom: atom, required: false),
            BeamContextForward481Bytes.Image(duplicate: true, atom: atom),
            BeamContextForward481Bytes.Image(duplicate: true, atom: atom, required: false),
            BeamContextForward481Bytes.Image(position: true, atom: atom),
            BeamContextForward481Bytes.Image(cycle: true, atom: atom) })
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
