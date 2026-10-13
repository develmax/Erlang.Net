namespace Erlang.Compiler;

public sealed record NativeRecordExpression(
    NativeRecordOperation Operation,
    string Module,
    string? Name,
    string Caller,
    bool External,
    Expr? Value,
    IReadOnlyList<RecordField> Fields
) : Expr
{
    public IReadOnlyDictionary<string, int> FieldOffsets { get; init; } = new Dictionary<string, int>();

    public int SourceOffset { get; init; }

    public NativeRecordDefinition? Declaration { get; init; }

    public IEnumerable<Expr> Children => (Value is null ? Enumerable.Empty<Expr>() : [Value]).Concat(Fields.Select(member => member.Value));
}
