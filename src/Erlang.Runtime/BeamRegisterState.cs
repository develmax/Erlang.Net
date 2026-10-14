namespace Erlang;

internal sealed class BeamRegisterState
{
    private readonly Term?[] values = new Term?[BeamOperandParameters.MaximumRegister];

    public Term Read(int index) => values[index] ?? throw new InvalidDataException(BeamRegisterNames.Uninitialized);

    public void Write(int index, Term value) => values[index] = value;

    public void CheckLive(int count)
    {
        for (int index = 0; index < count; index++)
        {
            _ = Read(index);
        }
    }
}
