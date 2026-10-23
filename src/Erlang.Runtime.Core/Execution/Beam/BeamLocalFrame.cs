namespace Erlang;

internal sealed class BeamLocalFrame
{
    private readonly BeamRegisterSlot[] values;
    private BeamFrameIdentity? identity;
    private int handlerCount;

    internal BeamLocalFrame(int slots) : this(slots, null, 0) { }

    private BeamLocalFrame(int slots, BeamFrameIdentity? identity, int handlerCount)
    {
        values = new BeamRegisterSlot[slots];
        this.identity = identity;
        this.handlerCount = handlerCount;
    }

    internal int Slots => values.Length;
    internal BeamFrameIdentity? Identity => identity;
    internal bool HasHandlers => handlerCount != 0;

    internal Term Read(int index) => values[index].ReadTerm();

    internal void Write(int index, Term value) => WriteSlot(index, BeamRegisterSlot.FromTerm(value));

    internal void InitializeBlank(int index) => Write(index, Nil.Value);

    internal BeamRegisterSlot ReadSlot(int index) => values[index];

    internal void WriteSlot(int index, BeamRegisterSlot value)
    {
        value.RequireMovable();
        if (values[index].Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        values[index] = value;
    }

    internal BeamHandlerTag InstallHandler(int index, BeamHandlerCheckpoint checkpoint)
    {
        if (values[index].Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        var tag = new BeamHandlerTag(identity ??= new BeamFrameIdentity(), checkpoint);
        values[index] = BeamRegisterSlot.FromHandler(tag);
        handlerCount++;

        return tag;
    }

    internal BeamHandlerTag ReadHandler(int index)
    {
        BeamHandlerTag tag = values[index].ReadHandler();
        if (!ReferenceEquals(tag.Frame, identity))
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);

        return tag;
    }

    internal int FindHandler(BeamHandlerTag tag)
    {
        if (handlerCount == 0 || !ReferenceEquals(tag.Frame, identity))
            return -1;
        for (int index = 0; index < values.Length; index++)
            if (values[index].Kind == BeamRegisterSlotKind.Handler && ReferenceEquals(values[index].ReadHandler(), tag))
                return index;

        return -1;
    }

    internal void RemoveHandler(int index, BeamHandlerTag expected)
    {
        if (!ReferenceEquals(ReadHandler(index), expected))
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        values[index] = BeamRegisterSlot.FromTerm(Nil.Value);
        handlerCount--;
    }

    internal BeamLocalFrame Trim(int removed, int remaining)
    {
        if (removed < 0 || remaining < 0 || removed > Slots || Slots - removed != remaining)
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        if (handlerCount != 0)
            for (int index = 0; index < removed; index++)
                if (values[index].Kind == BeamRegisterSlotKind.Handler)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        var trimmed = new BeamLocalFrame(remaining, identity, handlerCount);
        Array.Copy(
            values,
            removed,
            trimmed.values,
            0,
            remaining
        );

        return trimmed;
    }

    internal BeamLocalFrame Clone()
    {
        var copy = new BeamLocalFrame(values.Length, identity, handlerCount);
        Array.Copy(values, copy.values, values.Length);

        return copy;
    }
}
