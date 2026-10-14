namespace Erlang;

internal sealed class BeamRegisterState
{
    private readonly Term?[] values = new Term?[BeamOperandParameters.MaximumRegister];

    private readonly Stack<BeamLocalFrame> frames = new();

    internal void Allocate(int slots) => frames.Push(new(slots));

    internal void Deallocate(int slots)
    {
        if (!frames.TryPop(out BeamLocalFrame? frame) || frame.Slots != slots)
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
    }

    internal Term ReadY(int index) => frames.Peek().Read(index);

    internal void WriteY(int index, Term value) => frames.Peek().Write(index, value);

    internal void InitializeY(int index) => frames.Peek().InitializeBlank(index);

    internal void Write(BeamOperand operand, Term value)
    {
        if (operand.Kind == BeamOperandParameters.Y)
            WriteY((int)operand.Value, value);
        else
            Write((int)operand.Value, value);
    }

    public Term Read(int index) => values[index] ?? throw new InvalidDataException(BeamRegisterNames.Uninitialized);

    public void Write(int index, Term value) => values[index] = value;

    public BeamRegisterState Clone()
    {
        var clone = new BeamRegisterState();
        Array.Copy(values, clone.values, values.Length);
        foreach (BeamLocalFrame frame in frames.Reverse())
            clone.frames.Push(frame.Clone());

        return clone;
    }

    public void CheckLive(int count)
    {
        for (int index = 0; index < count; index++)
        {
            _ = Read(index);
        }
    }
}
