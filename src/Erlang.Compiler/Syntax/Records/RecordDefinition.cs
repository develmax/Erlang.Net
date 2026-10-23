namespace Erlang.Compiler;

public sealed record RecordDefinition(string Name, IReadOnlyList<RecordField> Fields);
