namespace Erlang;

public sealed record RuntimeHaltRequest(
    RuntimeHaltKind Kind,
    int Status,
    string? Slogan,
    RuntimeHaltOptions Options
)
{
    public Term? LogicalStack { get; init; }
}
