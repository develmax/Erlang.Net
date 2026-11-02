namespace Erlang;

/// <summary>Associates a field with immutable syntax metadata and an offset.</summary>
/// <remarks>
/// Syntax retains the exact caller-owned Term. Its C# reference does not root
/// physical cells or native backing; any supplying custody must outlive reads.
/// Grammar-produced detached syntax needs no additional native ownership.
/// </remarks>
public sealed record NativeRecordFieldType(Atom Field, Term Syntax, int SourceOffset);
