namespace Erlang;

internal sealed partial class BeamRegisterState : IDisposable
{
    private readonly BeamRegisterSlot[] values = new BeamRegisterSlot[BeamOperandParameters.MaximumRegister];
    private readonly Stack<BeamLocalFrame> frames = new();
    private readonly double?[] floating = new double?[BeamOperandParameters.MaximumRegister];
    private readonly ProcessWordExecution504? words504;
    private readonly ProcessContext? context507;
    private readonly BeamOwnedRoots507? roots507;

    internal BeamRegisterState() { }

    internal BeamRegisterState(ProcessWordExecution504? words, ProcessContext? context = null)
    {
        words504 = words;
        context507 = context;
        if (context is not null && context.OwnedExecutionEnabled683)
            roots507 = new(context, values.Length);
    }

    internal IDisposable? EnterWordOperation504() => words504?.Enter();

    internal double ReadFloat(BeamOperand operand)
        => floating[(int)operand.Value] ?? throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);

    internal void WriteFloat(BeamOperand operand, double value)
    {
        if (!double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);
        floating[(int)operand.Value] = value;
    }

    internal void ClearFloats() => Array.Clear(floating);

    internal int FrameDepth => frames.Count;

    internal void Allocate(int slots) => PushFrame507(new(slots, words504, context507));

    private void PushFrame507(BeamLocalFrame frame)
    {
        try
        {
            frames.Push(frame);
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    internal void Deallocate(int slots)
    {
        if (!frames.TryPeek(out BeamLocalFrame? frame) || frame.Slots != slots || frame.HasHandlers)
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        frames.Pop().Dispose();
    }

    internal Term ReadY(int index) => frames.Peek().Read(index);

    internal void WriteY(int index, Term value) => frames.Peek().Write(index, value);

    internal void InitializeY(int index) => frames.Peek().InitializeBlank(index);

    internal void Trim(int removed, int remaining)
    {
        if (!frames.TryPeek(out BeamLocalFrame? frame))
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        BeamLocalFrame trimmed = frame.Trim(removed, remaining);
        try
        {
            frames.Pop().Dispose();
            PushFrame507(trimmed);
        }
        catch
        {
            trimmed.Dispose();
            throw;
        }
    }

    internal void Write(BeamOperand operand, Term value)
    {
        if (operand.Kind == BeamOperandParameters.Y)
            WriteY((int)operand.Value, value);
        else
            Write((int)operand.Value, value);
    }

    public Term Read(int index) => values[index].ReadTerm();

    public void Write(int index, Term value) => WriteXSlot507(index, BeamRegisterSlot.FromTerm(value), true);

    internal BeamRegisterSlot ReadSlot(BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.X => values[(int)operand.Value],
        BeamOperandParameters.Y => frames.Peek().ReadSlot((int)operand.Value),
        _ => throw new InvalidDataException(BeamRegisterProtocolNames.TypedValue)
    };

    internal void WriteSlot(BeamOperand operand, BeamRegisterSlot value)
    {
        value.RequireMovable();
        if (operand.Kind == BeamOperandParameters.Y)
            frames.Peek().WriteSlot((int)operand.Value, value);
        else if (operand.Kind == BeamOperandParameters.X)
            WriteXSlot507((int)operand.Value, value, false);
        else
            throw new InvalidDataException(BeamRegisterProtocolNames.TypedValue);
    }

    internal void WriteReceiveMarker(BeamOperand destination, BeamReceiveMarker marker)
        => WriteSlot(destination, BeamRegisterSlot.FromReceiveMarker(marker));

    internal BeamReceiveMarker ReadReceiveMarker(BeamOperand source)
        => ReadSlot(source).ReadReceiveMarker();

    internal void WriteBinaryMatchContext(BeamOperand destination, BeamBinaryMatchContext context)
        => WriteSlot(destination, BeamRegisterSlot.FromBinaryMatchContext(context));

    internal BeamBinaryMatchContext ReadBinaryMatchContext(BeamOperand source)
        => ReadSlot(source).ReadBinaryMatchContext();

    internal void WriteBinaryPosition(BeamOperand destination, BeamBinaryMatchPosition position)
        => WriteSlot(destination, BeamRegisterSlot.FromBinaryPosition(position));

    internal BeamBinaryMatchPosition ReadBinaryPosition(BeamOperand source)
        => ReadSlot(source).ReadBinaryPosition();

    internal Term CaptureArgument(int index)
    {
        BeamRegisterSlot slot = values[index];
        if (slot.Kind == BeamRegisterSlotKind.BinaryMatchContext)
        {
            BeamBinaryMatchContext match = slot.ReadBinaryMatchContext();

            return match.Source.SliceBits(match.Position, match.Source.BitLength - match.Position);
        }

        return slot.ReadTerm();
    }

    internal void CopyRegister(BeamOperand source, BeamOperand destination)
        => WriteSlot(destination, ReadSlot(source));

    internal void SwapRegisters(BeamOperand first, BeamOperand second)
    {
        BeamRegisterSlot beforeFirst = ReadSlot(first);
        BeamRegisterSlot beforeSecond = ReadSlot(second);
        beforeFirst.RequireMovable();
        beforeSecond.RequireMovable();
        using var swapRoots507 = roots507?.Protect(Term.Tuple(beforeFirst.RootGraph507(), beforeSecond.RootGraph507()));
        WriteSlot(first, beforeSecond);
        WriteSlot(second, beforeFirst);
    }

    internal void WriteSavedTrace(int index, BeamSavedTrace trace) => WriteXSlot507(index, BeamRegisterSlot.FromSavedTrace(trace), false);

    internal BeamSavedTrace ReadSavedTrace(int index) => values[index].ReadSavedTrace();

    internal void WriteExceptionMarkerX(int index, BeamHandlerTag tag)
        => WriteXSlot507(index, BeamRegisterSlot.FromHandler(tag), false);

    internal void RequireExceptionMarker(int index, BeamHandlerTag expected)
    {
        if (!ReferenceEquals(values[index].ReadHandler(), expected))
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
    }

    internal void ClearXExcept(params int[] live)
    {
        if (live.Any(index => index < 0 || index >= values.Length))
            throw new InvalidDataException(BeamRegisterNames.Unsupported);
        if (roots507 is not null)
        {
            for (int index = 0; index < values.Length; index++)
                if (!live.Contains(index))
                {
                    values[index] = default;
                    roots507.Clear(index);
                }

            return;
        }
        var retained = new BeamRegisterSlot[live.Length];
        for (int index = 0; index < live.Length; index++)
            retained[index] = values[live[index]];
        Array.Clear(values);
        for (int index = 0; index < live.Length; index++)
            values[live[index]] = retained[index];
    }

    internal BeamHandlerTag InstallHandlerY(int index, BeamHandlerCheckpoint checkpoint)
    {
        if (checkpoint.RegisterFrameDepth != frames.Count || frames.Count == 0
            || checkpoint.ContinuationDepth < 0 || checkpoint.CallFrameDepth < 0 || checkpoint.FunctionScopeDepth < 0
            || checkpoint.HandlerInstructionIndex < checkpoint.Function.FirstInstructionIndex
            || checkpoint.HandlerInstructionIndex >= checkpoint.Function.EndInstructionIndex)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);

        return frames.Peek().InstallHandler(index, checkpoint);
    }

    internal BeamHandlerTag ReadHandlerY(int index) => frames.Peek().ReadHandler(index);

    internal void RemoveHandlerY(int index, BeamHandlerTag expected) => frames.Peek().RemoveHandler(index, expected);

    internal void RemoveHandler(BeamHandlerTag expected)
    {
        int index = frames.Peek().FindHandler(expected);
        if (index < 0)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        frames.Peek().RemoveHandler(index, expected);
    }

    internal void UnwindToHandler(BeamHandlerTag expected)
    {
        BeamLocalFrame? target = frames.FirstOrDefault(frame => frame.FindHandler(expected) >= 0);
        int discarded = frames.TakeWhile(frame => !ReferenceEquals(frame, target)).Count();
        if (target is null || expected.Checkpoint.RegisterFrameDepth != frames.Count - discarded)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        // Root must first select the nearest active tag. A live intervening tag
        // indicates inconsistent interpreter handler-stack selection.
        foreach (BeamLocalFrame frame in frames.TakeWhile(frame => !ReferenceEquals(frame, target)))
            if (frame.HasHandlers)
                throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        while (!ReferenceEquals(frames.Peek(), target))
            frames.Pop().Dispose();
    }

    public BeamRegisterState Clone()
    {
        var clone = new BeamRegisterState(words504, context507);
        try
        {
            if (roots507 is null)
                Array.Copy(values, clone.values, values.Length);
            else
                for (int index = 0; index < values.Length; index++)
                    if (values[index].Kind != BeamRegisterSlotKind.Uninitialized)
                        clone.WriteXSlot507(index, values[index], false);
            Array.Copy(floating, clone.floating, floating.Length);
            foreach (BeamLocalFrame frame in frames.Reverse())
                clone.PushFrame507(frame.Clone());

            return clone;
        }
        catch
        {
            clone.Dispose();
            throw;
        }
    }

    public void CheckLive(int count)
    {
        for (int index = 0; index < count; index++)
            values[index].RequireMovable();
    }

    public void Dispose()
    {
        if (roots507 is null)
        {
            while (frames.TryPop(out BeamLocalFrame? ordinary))
                ordinary.Dispose();
            Array.Clear(values);
            Array.Clear(floating);

            return;
        }
        List<Exception>? failures = null;
        while (frames.TryPop(out BeamLocalFrame? frame))
        {
            try
            {
                frame.Dispose();
            }
            catch (Exception failure)
            {
                (failures ??= new()).Add(failure);
            }
        }
        Array.Clear(values);
        Array.Clear(floating);
        try
        {
            roots507?.Dispose();
        }
        catch (Exception failure)
        {
            (failures ??= new()).Add(failure);
        }
        if (failures is not null)
            throw new AggregateException(failures);
    }
}
