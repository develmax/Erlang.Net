namespace Erlang;

internal sealed class BeamHandlerTag(BeamFrameIdentity frame, BeamHandlerCheckpoint checkpoint)
{
    internal BeamFrameIdentity Frame { get; } = frame;
    internal BeamHandlerCheckpoint Checkpoint { get; } = checkpoint;
}
