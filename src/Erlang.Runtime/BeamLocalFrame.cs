namespace Erlang;

internal sealed class BeamLocalFrame(int slots)
{
    private readonly Term?[] values = new Term?[slots];
    internal int Slots => values.Length;

    internal Term Read(int index) => values[index] ?? throw new InvalidDataException(BeamRegisterNames.Uninitialized);

    internal void Write(int index, Term value) => values[index] = value;

    internal void InitializeBlank(int index) => values[index] = Nil.Value;

    internal BeamLocalFrame Clone()
    {
        var copy = new BeamLocalFrame(values.Length);
        Array.Copy(values, copy.values, values.Length);

        return copy;
    }
}
