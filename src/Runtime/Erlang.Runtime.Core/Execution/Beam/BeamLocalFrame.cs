namespace Erlang;

internal sealed class BeamLocalFrame : IDisposable
{
    private readonly BeamRegisterSlot[]? values;
    private readonly ProcessOwnedWordLease504? storage504;
    private readonly ProcessWordExecution504? words504;
    private readonly int slots;
    private BeamFrameIdentity? identity;
    private int handlerCount;

    internal BeamLocalFrame(int slots) : this(
        slots,
        null,
        null,
        0
    )
    { }

    internal BeamLocalFrame(int slots, ProcessWordExecution504? words) : this(
        slots,
        words,
        null,
        0
    )
    { }

    private BeamLocalFrame(
        int slots,
        ProcessWordExecution504? words,
        BeamFrameIdentity? identity,
        int handlerCount
    )
    {
        this.slots = slots;
        words504 = words;
        if (words is null)
            values = new BeamRegisterSlot[slots];
        else
            storage504 = words.Allocate(slots);
        this.identity = identity;
        this.handlerCount = handlerCount;
    }

    internal int Slots => slots;
    internal BeamFrameIdentity? Identity => identity;
    internal bool HasHandlers => handlerCount != 0;

    internal Term Read(int index) => ReadSlot(index).ReadTerm();

    internal void Write(int index, Term value) => WriteSlot(index, BeamRegisterSlot.FromTerm(value));

    internal void InitializeBlank(int index) => Write(index, Nil.Value);

    internal BeamRegisterSlot ReadSlot(int index) => storage504 is null
        ? values![index] : BeamRegisterSlot.FromStoredValue504(storage504.Read(index));

    private void WriteRaw504(int index, BeamRegisterSlot value)
    {
        if (storage504 is null)
            values![index] = value;
        else
            storage504.Write(index, value.StoredValue504);
    }

    internal void WriteSlot(int index, BeamRegisterSlot value)
    {
        value.RequireMovable();
        if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        WriteRaw504(index, value);
    }

    internal BeamHandlerTag InstallHandler(int index, BeamHandlerCheckpoint checkpoint)
    {
        if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        var tag = new BeamHandlerTag(identity ??= new BeamFrameIdentity(), checkpoint);
        WriteRaw504(index, BeamRegisterSlot.FromHandler(tag));
        handlerCount++;

        return tag;
    }

    internal BeamHandlerTag ReadHandler(int index)
    {
        BeamHandlerTag tag = ReadSlot(index).ReadHandler();
        if (!ReferenceEquals(tag.Frame, identity))
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);

        return tag;
    }

    internal int FindHandler(BeamHandlerTag tag)
    {
        if (handlerCount == 0 || !ReferenceEquals(tag.Frame, identity))
            return -1;
        for (int index = 0; index < Slots; index++)
            if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler && ReferenceEquals(ReadSlot(index).ReadHandler(), tag))
                return index;

        return -1;
    }

    internal void RemoveHandler(int index, BeamHandlerTag expected)
    {
        if (!ReferenceEquals(ReadHandler(index), expected))
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        WriteRaw504(index, BeamRegisterSlot.FromTerm(Nil.Value));
        handlerCount--;
    }

    internal BeamLocalFrame Trim(int removed, int remaining)
    {
        if (removed < 0 || remaining < 0 || removed > Slots || Slots - removed != remaining)
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        if (handlerCount != 0)
            for (int index = 0; index < removed; index++)
                if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        var trimmed = new BeamLocalFrame(
            remaining,
            words504,
            identity,
            handlerCount
        );
        try
        {
            for (int index = 0; index < remaining; index++)
                trimmed.WriteRaw504(index, ReadSlot(index + removed));

            return trimmed;
        }
        catch
        {
            trimmed.Dispose();
            throw;
        }
    }

    internal BeamLocalFrame Clone()
    {
        var copy = new BeamLocalFrame(
            Slots,
            words504,
            identity,
            handlerCount
        );
        try
        {
            for (int index = 0; index < Slots; index++)
                copy.WriteRaw504(index, ReadSlot(index));

            return copy;
        }
        catch
        {
            copy.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (storage504 is not null)
            words504!.Release(storage504);
        else
            Array.Clear(values!);
    }
}
