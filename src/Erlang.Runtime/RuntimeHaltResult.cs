namespace Erlang;

public sealed record RuntimeHaltResult(int Status)
{
    public RuntimeHaltKind Kind { get; init; }
    public string? Slogan { get; init; }
    public Term? LogicalStack { get; init; }
}
